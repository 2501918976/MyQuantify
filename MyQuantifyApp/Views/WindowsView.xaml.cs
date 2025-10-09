using Microsoft.Web.WebView2.Core;
using MyQuantifyApp.Database;
using MyQuantifyApp.Database.Models;
using MyQuantifyApp.Database.Repositories.Raw;
using MyQuantifyApp.Services;
using Serilog;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;

namespace MyQuantifyApp.Views
{
    public class CategoryDetailDto
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public List<ProcessInfo> processes { get; set; } = new List<ProcessInfo>();
    }


    public partial class WindowsView : Page
    {
        private bool _isWebViewReady = false;
        private readonly SQLiteDataService _dbService;

        private CategoryRepository CategoryRepo => new CategoryRepository(_dbService.ConnectionString);
        private ProcessRepository ProcessRepo => new ProcessRepository(_dbService.ConnectionString);

        public WindowsView()
        {
            Log.Debug("🛠️ WindowsView 构造函数开始执行...");
            InitializeComponent();

            _dbService = new SQLiteDataService();
            Log.Debug("🛠️ SQLiteDataService 初始化完成，连接字符串: {ConnString}", _dbService.ConnectionString);

            this.Loaded += WindowsWebView_Loaded;
            Log.Debug("🛠️ WindowsView 构造函数执行完毕，已订阅 WindowsWebView_Loaded 事件。");
        }

        private async void WindowsWebView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Log.Debug("🛠️ WindowsWebView_Loaded 事件触发。");
            // 假设 WindowsWebView 是 XAML 中定义的 WebView2 控件
            if (WindowsWebView == null)
            {
                Log.Warning("⚠️ WindowsWebView 控件为 null。");
                return;
            }

            await WindowsWebView.EnsureCoreWebView2Async();
            Log.Debug("🛠️ CoreWebView2 初始化完成。");

            string subPath = System.IO.Path.Combine("wwwroot", "Windows.html");
            string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);
            string htmlUri = new Uri(htmlPath).AbsoluteUri;

            WindowsWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

            WindowsWebView.NavigationCompleted += (s, ev) =>
            {
                _isWebViewReady = true;
                Log.Information("🟢 DailyReport WebView2 页面加载完成: {Uri}", htmlUri);
            };

            WindowsWebView.WebMessageReceived += OnWebMessageReceived;
            Log.Debug("🛠️ 已订阅 WebMessageReceived 事件。");


            if (System.IO.File.Exists(htmlPath))
            {
                WindowsWebView.Source = new Uri(htmlUri);
                Log.Debug("🛠️ 开始导航到本地 HTML 文件: {Uri}", htmlUri);
            }
            else
            {
                WindowsWebView.NavigateToString("<h1>错误: 找不到 Windows.html 文件。</h1>");
                Log.Error("❌ 找不到 Windows.html 文件: {Path}", htmlPath);
            }

            // 发送测试命令
            //_ = Task.Run(async () =>
            //{
            //    await Task.Delay(2000);
            //    Log.Debug("🛠️ 延迟 2000ms 后，尝试发送 'notify' 测试命令...");
            //    await SendCommandAsync("notify", new { message = "C# 主动发送给日报页面的消息" });
            //});
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            Log.Debug("🛠️ 收到来自 JS 的 WebMessage 事件。");
            if (WindowsWebView?.CoreWebView2 == null) return;

            string? json = null;
            //try
            //{
            //    json = e.TryGetWebMessageAsString();
            //}
            //catch (Exception ex)
            //{
            //    Log.Warning(ex, "⚠️ TryGetWebMessageAsString 失败，尝试 WebMessageAsJson。");
                try
                {
                    json = e.WebMessageAsJson;
                }
                catch (Exception ex2)
                {
                    Log.Error(ex2, "❌ WebMessage 内容无法解析为字符串或 JSON。");
                    return;
                }
            //}

            if (string.IsNullOrEmpty(json))
            {
                Log.Debug("⚠️ 接收到的 JSON 消息为空。");
                return;
            }

            Log.Information("接收到 JS 消息: {Json}", json);

            WebMessage? msg = null;
            try
            {
                msg = JsonSerializer.Deserialize<WebMessage>(json);
                Log.Debug("🛠️ 消息已成功反序列化。");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ JSON 反序列化到 WebMessage 失败: {Json}", json);
                return;
            }

            if (msg == null) return;

            Log.Debug("处理命令: {Cmd} (ReqId={ReqId})", msg.Cmd, msg._reqId);

            object? responseData = null;
            JsonElement element = msg.Data;

            switch (msg.Cmd)
            {
                case "getTagsData":
                    {
                        Log.Information("Bridge: 正在获取分类数据...");
                        var rawData = CategoryRepo.GetAllCategoriesWithDetails();

                        // 🛠️ 【修改点 3：详细打印原始数据】
                        Log.Debug("🛠️ 原始数据包含 {Count} 个分类键值对。", rawData.Count);
                        foreach (var kvp in rawData)
                        {
                            Log.Debug("🛠️ Category: {Name} (ID: {Id}) - Process Count: {PCount}",
                                kvp.Key.Name, kvp.Key.Id, kvp.Value.Count);
                        }

                        // 修正：使用小写的 tags 属性名
                        var dtoList = ConvertCategoriesToDto(rawData); // 调用 ConvertCategoriesToDto
                        responseData = new { tags = dtoList };
                        Log.Debug("🛠️ 获取到 {Count} 个分类详情数据。", dtoList.Count);
                        break;
                    }

                case "selectTag":
                    {
                        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("tagName", out JsonElement tagNameElement) && tagNameElement.ValueKind == JsonValueKind.String)
                        {
                            string? tagName = tagNameElement.GetString();
                            if (tagName != null)
                            {
                                Log.Information("Bridge: 选中标签: {TagName}", tagName);
                                // 实际操作可能只是更新 C# 后端的选中状态，这里返回 Success = true 即可
                                responseData = new { Success = true };
                                break;
                            }
                        }
                        Log.Warning("⚠️ selectTag 命令参数错误。");
                        responseData = new { Success = false, Error = "参数错误" };
                        break;
                    }

                case "addTag":
                    {
                        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("tagName", out JsonElement tagNameElement) && tagNameElement.ValueKind == JsonValueKind.String)
                        {
                            string? tagName = tagNameElement.GetString();
                            if (tagName != null)
                            {
                                Log.Information("Bridge: 正在添加新标签: {TagName}", tagName);
                                var newCategory = new Category { Name = tagName };
                                try
                                {
                                    CategoryRepo.AddCategory(newCategory);
                                    Log.Debug("🛠️ 标签 {TagName} 已添加到数据库。", tagName);

                                    var rawData = CategoryRepo.GetAllCategoriesWithDetails();
                                    // 修正：使用小写的 tags 属性名
                                    responseData = new { Success = true, tags = ConvertCategoriesToDto(rawData) };

                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "❌ 添加标签失败: {TagName}", tagName);
                                    responseData = new { Success = false, Error = "数据库错误或标签已存在" };
                                }
                                break;
                            }
                        }
                        Log.Warning("⚠️ addTag 命令参数错误。");
                        responseData = new { Success = false, Error = "参数错误" };
                        break;
                    }

                case "deleteTag":
                    {
                        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("tagName", out JsonElement tagNameElement) && tagNameElement.ValueKind == JsonValueKind.String)
                        {
                            string? tagName = tagNameElement.GetString();
                            if (tagName != null)
                            {
                                Log.Warning("Bridge: 正在删除标签: {TagName}", tagName);

                                var categoryToDelete = CategoryRepo.GetAllCategories().FirstOrDefault(c => c.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

                                if (categoryToDelete != null && categoryToDelete.Id > 0)
                                {
                                    CategoryRepo.DeleteCategory(categoryToDelete.Id);
                                    Log.Debug("🛠️ 标签 {TagName} 已从数据库删除。", tagName);

                                    var rawData = CategoryRepo.GetAllCategoriesWithDetails();
                                    // 修正：使用小写的 tags 属性名
                                    responseData = new { Success = true, tags = ConvertCategoriesToDto(rawData) };
                                }
                                else
                                {
                                    Log.Warning("⚠️ 尝试删除标签失败，找不到匹配项或 ID 无效。TagName: {TagName}", tagName);
                                    responseData = new { Success = false, Error = "找不到标签或标签是 '未分类'" };
                                }
                                break;
                            }
                        }
                        Log.Warning("⚠️ deleteTag 命令参数错误。");
                        responseData = new { Success = false, Error = "参数错误" };
                        break;
                    }

                case "changeTag":
                    {
                        if (element.ValueKind == JsonValueKind.Object &&
                            element.TryGetProperty("processName", out JsonElement processNameElement) && processNameElement.ValueKind == JsonValueKind.String &&
                            element.TryGetProperty("newTagName", out JsonElement newTagNameElement) && newTagNameElement.ValueKind == JsonValueKind.String)
                        {
                            string? processName = processNameElement.GetString();
                            string? newTagName = newTagNameElement.GetString();

                            if (processName != null && newTagName != null)
                            {
                                Log.Information("Bridge: 重新分类进程: {ProcessName} 到 {NewTag}", processName, newTagName);

                                var newCategory = CategoryRepo.GetAllCategories().FirstOrDefault(c => c.Name.Equals(newTagName, StringComparison.OrdinalIgnoreCase));
                                int newCategoryId = newCategory?.Id ?? 0;
                                Log.Debug("🛠️ 进程 {ProcessName} 的新 Category ID 为: {NewId}", processName, newCategoryId);

                                 ProcessRepo.UpdateProcessCategory(processName, newCategoryId); 

                                // 重新加载数据并返回给前端
                                var rawData = CategoryRepo.GetAllCategoriesWithDetails();
                                var dtoList = ConvertCategoriesToDto(rawData);
                                responseData = new { Success = true, tags = dtoList };
                                break;
                            }
                        }
                        Log.Warning("⚠️ changeTag 命令参数错误。");
                        responseData = new { Success = false, Error = "参数错误" };
                        break;
                    }

                default:
                    Log.Warning("⚠️ 未知命令: {Cmd}，消息将被忽略。", msg.Cmd);
                    return;
            }

            // 统一发送响应
            if (msg._reqId.HasValue && responseData != null)
            {
                Log.Debug("🛠️ 准备发送响应 (ReqId: {ReqId})", msg._reqId.Value);
                await SendResponseAsync(msg._reqId, responseData);
            }
            else if (msg._reqId.HasValue && responseData == null)
            {
                Log.Warning("⚠️ 命令 {Cmd} 有 ReqId 但返回的 responseData 为 null。", msg.Cmd);
                await SendResponseAsync(msg._reqId, new { Success = false, Error = "内部错误：未生成响应数据" });
            }
        }

        private async Task SendResponseAsync(int? reqId, object data)
        {
            if (!reqId.HasValue) return;

            // 循环等待 WebView 就绪
            while (!_isWebViewReady || WindowsWebView.CoreWebView2 == null)
            {
                Log.Debug("🛠️ WebView 尚未就绪，等待 50ms 后重试发送响应 (ReqId: {ReqId})...", reqId.Value);
                await Task.Delay(50);
            }

            var response = new { _resId = reqId.Value, data };
            string json = JsonSerializer.Serialize(response);

            // 🛠️ 【修改点 1：打印完整的 JSON 响应】
            Log.Debug("🛠️ 响应数据序列化成功 (ReqId: {ReqId}): {Json}", reqId.Value, json); // 打印完整 JSON
            // 替换旧的：Log.Debug("🛠️ 响应数据序列化成功: {JsonSegment}...", json.Length > 100 ? json[..100] : json);

            try
            {
                WindowsWebView.CoreWebView2.PostWebMessageAsString(json);
                Log.Information("🟢 已发送响应给 JS (ReqId: {ReqId})", reqId.Value);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ 发送响应失败 (ReqId: {ReqId})", reqId.Value);
            }
        }

        private async Task SendCommandAsync(string cmd, object data)
        {
            // 循环等待 WebView 就绪
            while (!_isWebViewReady || WindowsWebView.CoreWebView2 == null)
            {
                Log.Debug("🛠️ WebView 尚未就绪，等待 50ms 后重试发送命令 ({Cmd})...", cmd);
                await Task.Delay(50);
            }

            var msg = new { cmd, data };
            string json = JsonSerializer.Serialize(msg);
            Log.Debug("🛠️ 命令数据序列化成功: {JsonSegment}...", json.Length > 100 ? json[..100] : json);


            try
            {
                WindowsWebView.CoreWebView2.PostWebMessageAsString(json);
                Log.Information("🟢 已发送命令给 JS: {Cmd}", cmd);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ 发送命令失败: {Cmd}", cmd);
            }
        }

        /// <summary>
        /// 将字典结构转换为适合 System.Text.Json 序列化的 List<CategoryDetailDto>
        /// </summary>
        private List<CategoryDetailDto> ConvertCategoriesToDto(Dictionary<Category, List<ProcessInfo>> data)
        {
            Log.Debug("🛠️ 开始将 Dictionary<Category, List<ProcessInfo>> 转换为 List<CategoryDetailDto>...");

            var dtoList = data
                .Select(kvp =>
                {
                    // 🛠️ 【修改点 2：在 DTO 转换中打印进程数量】
                    Log.Debug("🛠️ 转换 DTO: {Name} (ID: {Id}) 进程数: {PCount}", kvp.Key.Name, kvp.Key.Id, kvp.Value.Count);

                    return new CategoryDetailDto
                    {
                        id = kvp.Key.Id,
                        name = kvp.Key.Name,
                        processes = kvp.Value
                    };
                })
                .OrderBy(d => d.id)
                .ToList();

            Log.Debug("🛠️ 转换完成，共生成 {Count} 个 DTO 对象。", dtoList.Count);
            return dtoList;
        }
    }
}