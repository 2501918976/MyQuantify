using Microsoft.Web.WebView2.Core;
using Serilog;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using MyQuantifyApp.Services;
namespace MyQuantifyApp.Views
{
    public partial class DailyReportView : Page
    {
        private bool _isWebViewReady = false;

        public DailyReportView()
        {
            InitializeComponent();
            Loaded += DailyReportView_Loaded;
        }

        private async void DailyReportView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DailyReportWebView == null) return;

            await DailyReportWebView.EnsureCoreWebView2Async();

            string subPath = System.IO.Path.Combine("wwwroot", "Day.html");
            string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);
            string htmlUri = new Uri(htmlPath).AbsoluteUri;

            DailyReportWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

            DailyReportWebView.NavigationCompleted += (s, ev) =>
            {
                _isWebViewReady = true;
                //Log.Information("🟢 DailyReport WebView2 页面加载完成: {Uri}", htmlUri);
            };

            DailyReportWebView.WebMessageReceived += OnWebMessageReceived;

            if (System.IO.File.Exists(htmlPath))
                DailyReportWebView.Source = new Uri(htmlUri);
            else
                DailyReportWebView.NavigateToString("<h1>错误: 找不到 Day.html 文件。</h1>");
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (DailyReportWebView?.CoreWebView2 == null) return;

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
                    //Log.Error(ex, "❌ DailyReport WebMessage 内容非法");
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

            if (msg == null) return;

            //Log.Debug("处理命令: {Cmd} _reqId={ReqId}", msg.Cmd, msg._reqId);

            switch (msg.Cmd)
            {
                case "getDailyData":
                    // 返回完整模拟日报数据
                    var random = new Random();

                    // 仪表盘
                    double total = Math.Round(random.NextDouble() * 5 + 8, 1); // 8~13 小时
                    double work = Math.Round(random.NextDouble() * 3 + 4, 1);  // 4~7 小时
                    double game = Math.Round(random.NextDouble() * 1.5 + 1, 1); // 1~2.5 小时
                    double afk = Math.Round(random.NextDouble() * 2 + 2, 1); // 2~4 小时
                    double other = Math.Round(Math.Max(0.1, total - work - game - afk), 1);
                    double typingCount = Math.Round(work * 15 + random.NextDouble() * 10, 1); // 千次
                    int copyCount = (int)(work * 10 + random.Next(100, 200));

                    // 混合图
                    int[] typingData = new int[24];
                    int[] copyData = new int[24];
                    for (int h = 0; h < 24; h++)
                    {
                        typingData[h] = h >= 9 && h <= 12 ? 50 + random.Next(0, 30) :
                                         h >= 14 && h <= 18 ? 70 + random.Next(0, 40) :
                                         h >= 20 && h <= 22 ? 20 + random.Next(0, 20) : 0;

                        copyData[h] = h >= 10 && h <= 17 ? 10 + random.Next(0, 10) : 0;
                    }

                    // 饼图一：时间占比
                    var timePie = new object[]
                    {
        new { value = work, name = "工作" },
        new { value = game, name = "游戏" },
        new { value = afk, name = "AFK / 休息" },
        new { value = other, name = "其他 (学习/社交)" }
                    };

                    // 饼图二：应用占比 (TOP 5)
                    var appPie = new object[]
                    {
        new { value = Math.Round(work * 0.4, 1), name = "Vvs Code / IDE" },
        new { value = Math.Round(work * 0.3, 1), name = "浏览器 (Chrome/Edge)" },
        new { value = Math.Round(work * 0.15,1), name = "聊天工具 (WeChat/Slack)" },
        new { value = Math.Round(game * 0.6,1), name = "游戏应用 (Steam/LOL)" },
        new { value = Math.Round(other * 0.5,1), name = "文档 / PDF" }
                    };

                    var completeFakeData = new
                    {
                        total,
                        work,
                        game,
                        afk,
                        typingCount,
                        copyCount,
                        typingData,
                        copyData,
                        timePie,
                        appPie
                    };

                    _ = SendResponseAsync(msg._reqId, completeFakeData);
                    //Log.Information("✅ 返回完整 getDailyData 数据, _reqId={ReqId}", msg._reqId);
                    break;


                default:
                    //Log.Warning("⚠️ 未知命令: {Cmd}", msg.Cmd);
                    break;
            }
        }

        private async Task SendResponseAsync(int? reqId, object data)
        {
            if (!reqId.HasValue) return;

            while (!_isWebViewReady || DailyReportWebView.CoreWebView2 == null)
                await Task.Delay(50);

            var response = new { _resId = reqId.Value, data };
            string json = JsonSerializer.Serialize(response);

            try
            {
                DailyReportWebView.CoreWebView2.PostWebMessageAsString(json);
                //Log.Information("🟢 已发送响应给 JS: {Json}", json);
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ 发送响应失败: {Json}", json);
            }
        }

        private async Task SendCommandAsync(string cmd, object data)
        {
            while (!_isWebViewReady || DailyReportWebView.CoreWebView2 == null)
                await Task.Delay(50);

            var msg = new { cmd, data };
            string json = JsonSerializer.Serialize(msg);

            try
            {
                DailyReportWebView.CoreWebView2.PostWebMessageAsString(json);
                //Log.Information("🟢 已发送命令给 JS: {Json}", json);
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ 发送命令失败: {Json}", json);
            }
        }
    }
}
