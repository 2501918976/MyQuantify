using Microsoft.Web.WebView2.Core;
using MyQuantifyApp.Database;
using MyQuantifyApp.Database.Repositories.Aggre;
using MyQuantifyApp.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using MyQuantifyApp.Views.Utils;

namespace MyQuantifyApp.Views
{
    // DTOs (Data Transfer Objects) 保持不变
    public class CategoryTimeEntry
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public double Hours { get; set; }
        public double TotalActivityDuration { get; set; }
        public double TotalAppDuration { get; set; }
    }
    public class PieChartDataDynamic
    {
        public string Date { get; set; }
        public List<CategoryTimeEntry> Entries { get; set; }
    }

    public class GetPieChartDataParams
    {
        public int days { get; set; }
    }

    public partial class PieChartView : Page
    {
        private bool _isWebViewReady = false;
        private readonly SQLiteDataService _dbService;
        private readonly CategoryTimeStatsRepository _categoryRepository;
        private readonly ProcessTimeStatsRepository _processRepository;


        public PieChartView()
        {
            InitializeComponent();
            this.Loaded += PieChartWebView_Loaded;
            _dbService = new SQLiteDataService();

            // 【修改 1】：初始化 Repositories
            // 使用数据库连接字符串初始化存储库，以确保数据访问功能可用。
            string connectionString = _dbService.ConnectionString;
            _categoryRepository = new CategoryTimeStatsRepository(connectionString);
            _processRepository = new ProcessTimeStatsRepository(connectionString);
        }

        private async void PieChartWebView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (PieChartWebView == null) return;

            await PieChartWebView.EnsureCoreWebView2Async();

            string subPath = System.IO.Path.Combine("Views", "wwwroot", "PieChart.html");
            string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);
            string htmlUri = new Uri(htmlPath).AbsoluteUri;

            PieChartWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

            PieChartWebView.NavigationCompleted += (s, ev) =>
            {
                _isWebViewReady = true;
                Log.Information("🟢 PieChartView.xaml WebView2 页面加载完成: {Uri}", htmlUri);
            };

            PieChartWebView.WebMessageReceived += OnWebMessageReceived;

            if (System.IO.File.Exists(htmlPath))
                PieChartWebView.Source = new Uri(htmlUri);
            else
                PieChartWebView.NavigateToString("<h1>错误: 找不到 PieChart.html 文件。</h1>");
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (PieChartWebView?.CoreWebView2 == null) return;

            string? json = null;
            try
            {
                json = e.TryGetWebMessageAsString();
            }
            catch { return; }

            if (string.IsNullOrEmpty(json)) return;

            // 使用 Log.Debug 记录接收到的 JSON
            Log.Debug("接收到 JS 消息: {Json}", json);

            WebMessage? msg = null;
            try
            {
                // 使用默认选项，确保可以解析到 JsonElement
                msg = JsonSerializer.Deserialize<WebMessage>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ JSON 解析失败: {Json}", json);
                return;
            }

            if (msg == null) return;

            Log.Debug("处理命令: {Cmd} _reqId={ReqId}", msg.Cmd, msg._reqId);

            switch (msg.Cmd)
            {
                case "getPieChartData":
                case "getAggregatedTimeData": // 【修改 3】：新增聚合数据命令处理
                    GetPieChartDataParams? requestParams = null;
                    try
                    {
                        requestParams = msg.Data.Deserialize<GetPieChartDataParams>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "❌ 解析 {Cmd} 参数失败", msg.Cmd);
                        return;
                    }

                    int days = requestParams?.days ?? 30;
                    object responseData;

                    if (msg.Cmd == "getPieChartData")
                    {
                        // 1. 处理每日明细数据 (原逻辑)
                        responseData = await GetRealPieChartData(days);
                        Log.Information("✅ 返回 getPieChartData 数据, 共 {Count} 天", (responseData as List<PieChartDataDynamic>)?.Count ?? 0);
                    }
                    else // msg.Cmd == "getAggregatedTimeData"
                    {
                        // 2. 处理聚合总数据 (新逻辑)
                        responseData = await GetAggregatedTimeData(days);
                        Log.Information("✅ 返回 getAggregatedTimeData 数据, 共 {Count} 条", (responseData as List<CategoryTimeEntry>)?.Count ?? 0);
                    }

                    // 响应给前端
                    await SendResponseAsync(msg._reqId, new { data = responseData });
                    break;

                default:
                    Log.Warning("⚠️ 未知命令: {Cmd}", msg.Cmd);
                    break;
            }
        }

        /// <summary>
        /// 从数据库获取指定天数的分类和进程时间数据，并转换为前端所需的 DTO 结构 (每日明细)。
        /// </summary>
        /// <param name="days">需要查询的天数。</param>
        /// <returns>动态的 PieChart 数据列表 (包含每日数据)。</returns>
        private async Task<List<PieChartDataDynamic>> GetRealPieChartData(int days)
        {
            if (_categoryRepository == null || _processRepository == null)
            {
                Log.Error("Repository 未初始化，无法获取数据。");
                return new List<PieChartDataDynamic>();
            }

            DateTime endDate = DateTime.Today;
            // +1 是为了包含 endDate 这一天
            DateTime startDate = endDate.AddDays(-days + 1);

            string startStr = startDate.ToString("yyyy-MM-dd");
            string endStr = endDate.ToString("yyyy-MM-dd");

            Log.Debug("开始从数据库获取分类和进程数据，日期范围: {Start} 到 {End}", startStr, endStr);

            // 并行获取分类和进程数据
            var categoryTask = Task.Run(() => _categoryRepository.GetCategoryTimeRangeWithNames(startStr, endStr));
            var processTask = Task.Run(() => _processRepository.GetProcessTimeRangeWithNames(startStr, endStr));

            await Task.WhenAll(categoryTask, processTask);

            var categoryData = categoryTask.Result;
            var processData = processTask.Result;

            // 合并所有日期键，并确保按日期排序
            var allDates = categoryData.Keys.Union(processData.Keys).OrderBy(d => d).ToList();
            var resultList = new List<PieChartDataDynamic>();

            foreach (var date in allDates)
            {
                // 获取当日的分类和进程数据 (如果不存在则使用空字典)
                var categorySeconds = categoryData.GetValueOrDefault(date, new Dictionary<string, int>());
                var processSeconds = processData.GetValueOrDefault(date, new Dictionary<string, int>());

                // 1. 计算当日总时长 (秒)
                long totalActivitySeconds = categorySeconds.Values.Sum(s => (long)s);
                long totalProcessSeconds = processSeconds.Values.Sum(s => (long)s);

                // 2. 转换为小时并四舍五入
                double totalActivityHours = Math.Round(totalActivitySeconds / 3600.0, 1);
                double totalAppHours = Math.Round(totalProcessSeconds / 3600.0, 1);

                var entries = new List<CategoryTimeEntry>();

                // --- A. 处理分类数据 (Type="Activity") ---
                foreach (var category in categorySeconds)
                {
                    double hours = category.Value / 3600.0; // 秒转换为小时

                    entries.Add(new CategoryTimeEntry
                    {
                        Type = "Activity", // 标记为 Activity/分类
                        Name = category.Key,
                        Hours = Math.Round(hours, 1),
                        TotalActivityDuration = totalActivityHours,
                        TotalAppDuration = totalAppHours
                    });
                }

                // --- B. 处理进程数据 (Type="App") ---
                foreach (var process in processSeconds)
                {
                    double hours = process.Value / 3600.0; // 秒转换为小时

                    entries.Add(new CategoryTimeEntry
                    {
                        Type = "App", // 标记为 App/进程
                        Name = process.Key,
                        Hours = Math.Round(hours, 1),
                        TotalActivityDuration = totalActivityHours,
                        TotalAppDuration = totalAppHours
                    });
                }

                resultList.Add(new PieChartDataDynamic { Date = date, Entries = entries });
            }

            Log.Debug("✅ 已成功从数据库获取并合并 {Count} 天的分类和进程数据 (每日明细)。", resultList.Count);
            // 返回按日期排序的列表
            return resultList.OrderBy(d => d.Date).ToList();
        }

        // 【修改 2】：新增聚合数据方法，用于饼图展示总时长
        /// <summary>
        /// 从数据库获取指定天数范围内的所有分类和进程时间，并聚合为总时长。
        /// </summary>
        /// <param name="days">需要聚合的总天数。</param>
        /// <returns>聚合后的 CategoryTimeEntry 列表。</returns>
        private async Task<List<CategoryTimeEntry>> GetAggregatedTimeData(int days)
        {
            // 步骤 1: 获取每日明细数据
            var dailyDataList = await GetRealPieChartData(days);

            // 步骤 2: 聚合所有日期的条目
            // Key: {Type}_{Name}, Value: CategoryTimeEntry (用于累加 Hours)
            var aggregatedMap = new Dictionary<string, CategoryTimeEntry>();

            foreach (var dailyData in dailyDataList)
            {
                foreach (var entry in dailyData.Entries)
                {
                    var key = $"{entry.Type}_{entry.Name}";

                    if (aggregatedMap.TryGetValue(key, out var existingEntry))
                    {
                        // 累加小时数
                        existingEntry.Hours += entry.Hours;
                    }
                    else
                    {
                        // 创建新条目
                        aggregatedMap.Add(key, new CategoryTimeEntry
                        {
                            Type = entry.Type,
                            Name = entry.Name,
                            Hours = entry.Hours,
                            // Totals will be recalculated based on the aggregated results
                            TotalActivityDuration = 0,
                            TotalAppDuration = 0
                        });
                    }
                }
            }

            var resultList = aggregatedMap.Values.ToList();

            // 步骤 3: 重新计算总时长并更新所有条目
            // 此时 resultList 中的 Hours 已经是聚合后的总时长。
            double totalAggregatedActivityHours = resultList
                .Where(e => e.Type == "Activity")
                .Sum(e => e.Hours);

            double totalAggregatedAppHours = resultList
                .Where(e => e.Type == "App")
                .Sum(e => e.Hours);

            // 更新 TotalActivityDuration 和 TotalAppDuration 字段
            foreach (var entry in resultList)
            {
                // 将聚合后的总小时数四舍五入
                entry.Hours = Math.Round(entry.Hours, 1);
                entry.TotalActivityDuration = Math.Round(totalAggregatedActivityHours, 1);
                entry.TotalAppDuration = Math.Round(totalAggregatedAppHours, 1);
            }

            Log.Debug("✅ 已成功获取并聚合 {Days} 天的分类和进程数据。", days);

            // 返回按小时数降序排列的列表，方便饼图展示
            return resultList
                .Where(e => e.Hours > 0) // 过滤掉 0 小时的条目
                .OrderByDescending(e => e.Hours)
                .ToList();
        }

        private async Task SendResponseAsync(int? reqId, object data)
        {
            if (!reqId.HasValue) return;

            while (!_isWebViewReady || PieChartWebView.CoreWebView2 == null)
                await Task.Delay(50);

            var response = new { _resId = reqId.Value, data };
            string json = JsonSerializer.Serialize(response);

            try
            {
                PieChartWebView.CoreWebView2.PostWebMessageAsString(json);
                // 使用 Log.Debug 记录响应
                Log.Debug("🟢 已发送响应给 JS: {Json}", json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ 发送响应失败: {Json}", json);
            }
        }

        private async Task SendCommandAsync(string cmd, object data)
        {
            while (!_isWebViewReady || PieChartWebView.CoreWebView2 == null)
                await Task.Delay(50);

            var msg = new { cmd, data };
            string json = JsonSerializer.Serialize(msg);

            try
            {
                PieChartWebView.CoreWebView2.PostWebMessageAsString(json);
                // 使用 Log.Debug 记录命令
                Log.Debug("🟢 已发送命令给 JS: {Json}", json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ 发送命令失败: {Json}", json);
            }
        }
    }
}
