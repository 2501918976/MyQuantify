using Microsoft.Web.WebView2.Core;
using MyQuantifyApp.Database;
using MyQuantifyApp.Database.Repositories.Raw;
using MyQuantifyApp.Services;
using Serilog;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using MyQuantifyApp.Views.Utils;

namespace MyQuantifyApp.Views
{
    public partial class CopyboardView : Page
    {
        private bool _isWebViewReady = false;
        private readonly SQLiteDataService _dbService;

        public CopyboardView()
        {
            InitializeComponent();
            Loaded += CopyboardView_Loaded;
            _dbService = new SQLiteDataService();
        }

        private async void CopyboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await CopyboardWebView.EnsureCoreWebView2Async();

            string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "wwwroot", "Clipboard.html");
            string htmlUri = new Uri(htmlPath).AbsoluteUri;

            CopyboardWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

            CopyboardWebView.NavigationCompleted += (s, ev) =>
            {
                _isWebViewReady = true;
                Log.Information("🟢 WebView2 页面加载完成: {Uri}", htmlUri);
            };

            CopyboardWebView.WebMessageReceived += OnWebMessageReceived;

            CopyboardWebView.Source = new Uri(htmlUri);
        }
        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (CopyboardWebView?.CoreWebView2 == null) return;

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
                    Log.Error(ex, "❌ WebMessage 内容非法");
                    return;
                }
            }

            if (string.IsNullOrEmpty(json)) return;

            WebMessage? msg = null;
            try
            {
                msg = JsonSerializer.Deserialize<WebMessage>(json);
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
                case "queryHistory":
                    {
                        var repo = new ClipboardActivityDataRepository(_dbService.ConnectionString);

                        var startDate = DateTime.Now.AddDays(-7);
                        var endDate = DateTime.Now;

                        var history = repo.GetClipboardLogsInRange(startDate, endDate, maxLength: 80);

                        var result = history.Select(h => new
                        {
                            id = h.Id.ToString(),
                            date = h.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                            length = h.Length,
                            content = h.Content
                        }).ToList();

                        _ = SendResponseAsync(msg._reqId, result);
                        Log.Information("✅ 返回 queryHistory 数据, 共 {Count} 条", result.Count);
                        break;
                    }

                case "getFullContent":
                    {
                        var id = msg.Data.GetProperty("id").GetString();
                        if (int.TryParse(id, out var contentId))
                        {
                            var repo = new ClipboardActivityDataRepository(_dbService.ConnectionString);
                            var fullContent = repo.GetFullClipboardContentById(contentId) ?? "(内容为空)";
                            _ = SendResponseAsync(msg._reqId, fullContent);
                            Log.Information("✅ 返回 getFullContent 数据, Id={Id}", contentId);
                        }
                        else
                        {
                            Log.Warning("⚠️ 无效的 ID: {Id}", id);
                        }
                        break;
                    }

                default:
                    Log.Warning("⚠️ 未知命令: {Cmd}", msg.Cmd);
                    break;
            }
        }
        private async Task SendResponseAsync(int? reqId, object data)
        {
            if (!reqId.HasValue) return;

            while (!_isWebViewReady || CopyboardWebView.CoreWebView2 == null)
            {
                await Task.Delay(50);
            }

            var response = new { _resId = reqId.Value, data };
            string json = JsonSerializer.Serialize(response);

            try
            {
                CopyboardWebView.CoreWebView2.PostWebMessageAsString(json);
                Log.Information("🟢 已发送响应给 JS: {Json}", json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ 发送响应失败: {Json}", json);
            }
        }
        private async Task SendCommandAsync(string cmd, object data)
        {
            while (!_isWebViewReady || CopyboardWebView.CoreWebView2 == null)
            {
                await Task.Delay(50);
            }

            var msg = new { cmd, data };
            string json = JsonSerializer.Serialize(msg);

            try
            {
                CopyboardWebView.CoreWebView2.PostWebMessageAsString(json);
                Log.Information("🟢 已发送命令给 JS: {Json}", json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ 发送命令失败: {Json}", json);
            }
        }
    }
}
