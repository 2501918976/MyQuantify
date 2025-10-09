using Microsoft.Web.WebView2.Core;
using MyQuantifyApp.Database;
using MyQuantifyApp.Database.Models.Aggre;
using MyQuantifyApp.Database.Repositories.Aggre;
using MyQuantifyApp.Database.Repositories.Raw;
using MyQuantifyApp.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using MyQuantifyApp.Views.Utils;

namespace MyQuantifyApp.Views
{
    public class LineChartDataPoint
    {
        public string Date { get; set; }
        public int typingCount { get; set; }
        public int copyCount { get; set; }
        public double total { get; set; }
        public double work { get; set; }
        public double game { get; set; }
        public double afk { get; set; }
    }

    public partial class LineChartView : Page
    {
        private bool _isWebViewReady = false;
        private readonly SQLiteDataService _dbService;

        private DailySummaryRepository _repository => new DailySummaryRepository(_dbService.ConnectionString);
        public LineChartView()
        {
            InitializeComponent();
            _dbService = new SQLiteDataService();


            Loaded += LineChartView_Loaded;
            Unloaded += LineChartView_Unloaded;
        }

        private async void LineChartView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (LineChartWebView == null) return;

            await LineChartWebView.EnsureCoreWebView2Async();

            string subPath = System.IO.Path.Combine("Views", "wwwroot", "LineChart.html");
            string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);
            string htmlUri = new Uri(htmlPath).AbsoluteUri;

            LineChartWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

            LineChartWebView.NavigationCompleted += (s, ev) =>
            {
                _isWebViewReady = true;
                //Log.Information("🟢 LineChart WebView2 页面加载完成: {Uri}", htmlUri);
            };

            LineChartWebView.WebMessageReceived += OnWebMessageReceived;

            if (System.IO.File.Exists(htmlPath))
                LineChartWebView.Source = new Uri(htmlUri);
            else
                LineChartWebView.NavigateToString("<h1>错误: 找不到 LineChart.html 文件。</h1>");
        }

        private void LineChartView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (LineChartWebView?.CoreWebView2 != null)
            {
                LineChartWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (LineChartWebView?.CoreWebView2 == null) return;

            string? json = null;
            try
            {
                json = e.TryGetWebMessageAsString();
            }
            catch
            {
                try
                {
                    json = e.WebMessageAsJson;
                }
                catch (Exception ex)
                {
                    //Log.Error(ex, "❌ LineChart WebMessage 内容非法");
                    return;
                }
            }

            if (string.IsNullOrEmpty(json)) return;

            //Log.Information("接收到 JS 消息: {Json}", json);

            WebMessage? msg = null;
            try
            {
                msg = JsonSerializer.Deserialize<WebMessage>(json);
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ JSON 解析失败: {Json}", json);
                return;
            }

            if (msg == null || string.IsNullOrEmpty(msg.Cmd) || !msg._reqId.HasValue) return;

            //Log.Debug("处理命令: {Cmd} _reqId={ReqId}", msg.Cmd, msg._reqId);

            switch (msg.Cmd)
            {
                case "getLineChartData":
                    _ = HandleGetLineChartData(msg._reqId.Value);
                    break;

                default:
                    //Log.Warning("⚠️ 未知命令: {Cmd}", msg.Cmd);
                    break;
            }
        }

        // 辅助方法：秒转换为小时 (保留一位小数)
        private double SecondsToHours(int seconds) => Math.Round(seconds / 3600.0, 1);

        /// <summary>
        /// 核心方法：响应 JS 的 getLineChartData 命令
        /// </summary>
        private async Task HandleGetLineChartData(int reqId)
        {
            List<LineChartDataPoint> realData = new List<LineChartDataPoint>();
            bool success = false;

            try
            {
                // 1. 直接调用仓储获取原始数据
                List<DailySummary> summaries = _repository.GetLast30Days();

                // 调试打印：检查从仓储获取的原始数据数量
                //Log.Debug("🔧 从仓储获取到原始数据 {Count} 条。", summaries?.Count ?? 0);

                // 2. 在 UI 层进行数据转换 (业务逻辑嵌入 UI 层)
                if (summaries != null && summaries.Count > 0)
                {
                    realData = summaries.Select(s => new LineChartDataPoint
                    {
                        Date = s.Date,
                        typingCount = s.KeyCount,
                        copyCount = s.CopyCount,
                        // 总使用时间 = 总活跃时间 + AFK 时间
                        total = SecondsToHours(s.TotalActiveSeconds + s.AfkSeconds),
                        work = SecondsToHours(s.WorkSeconds),
                        game = SecondsToHours(s.GameSeconds),
                        afk = SecondsToHours(s.AfkSeconds)
                    }).ToList();

                    success = realData.Count > 0;

                    // 调试打印：检查转换后的数据数量
                    //Log.Debug("🔧 转换后得到 LineChartDataPoint {Count} 条。", realData.Count);

                    // 调试打印：打印前3条数据，检查内容
                    if (realData.Count > 0)
                    {
                        // 打印序列化后的前3条数据（避免打印整个大列表）
                        var sampleData = realData.Take(Math.Min(realData.Count, 3)).ToList();
                        //Log.Debug("🔧 前 {Count} 条转换后的数据示例: {Data}", sampleData.Count, JsonSerializer.Serialize(sampleData));
                    }
                }
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ 调用仓储获取 LineChartData 失败");
            }

            // 无论成功与否，都向 JS 发送响应。
            var responseData = new { data = realData };
            await SendResponseAsync(reqId, responseData);

            if (success)
            {
                //Log.Information("✅ 返回 getLineChartData 数据, _reqId={ReqId}, Count={Count}", reqId, realData.Count);
            }
            else
            {
                //Log.Warning("⚠️ Bridge 调用成功但数据为空或失败，返回空数组给 JS。 _reqId={ReqId}", reqId);
            }
        }

        private async Task SendResponseAsync(int reqId, object data)
        {
            while (!_isWebViewReady || LineChartWebView.CoreWebView2 == null)
                await Task.Delay(50);
            var response = new { _resId = reqId, data };
            string json = JsonSerializer.Serialize(response);

            //Log.Debug("🔧 准备发送给 JS 的完整 JSON 响应: {Json}", json.Length > 200 ? json.Substring(0, 200) + "..." : json);

            try
            {
                LineChartWebView.CoreWebView2.PostWebMessageAsString(json);
                //Log.Information("🟢 已发送响应给 JS: _resId={ReqId}", reqId);
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ 发送响应失败: {Json}", json);
            }
        }

        // ... (SendCommandAsync 方法保持不变) ...
        private async Task SendCommandAsync(string cmd, object data)
        {
            while (!_isWebViewReady || LineChartWebView.CoreWebView2 == null)
                await Task.Delay(50);

            var msg = new { cmd, data };
            string json = JsonSerializer.Serialize(msg);

            try
            {
                LineChartWebView.CoreWebView2.PostWebMessageAsString(json);
                //Log.Information("🟢 已发送命令给 JS: {Cmd}", cmd);
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ 发送命令失败: {Json}", json);
            }
        }
    }
}