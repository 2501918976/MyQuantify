using MyQuantifyApp.Models.Day;
using System;
using System.Collections.Generic;
using System.IO; // 包含 System.IO.Path 和 System.IO.File
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyQuantifyApp.Views
{
    /// <summary>
    /// DailyReportView.xaml 的交互逻辑
    /// </summary>
    public partial class DailyReportView : Page
    {
        public DailyReportView()
        {
            InitializeComponent();
            this.Loaded += DailyReportView_Loaded;
        }

        private async void DailyReportView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DailyReportWebView != null)
            {
                // 1. 初始化 WebView2
                await DailyReportWebView.EnsureCoreWebView2Async();

                // 2. 构建目标 HTML 文件的路径
                // 目标路径：[应用程序根目录]/assert/html/day.html
                // 重点修改：将 Path.Combine 明确改为 System.IO.Path.Combine
                string subPath = System.IO.Path.Combine("assets", "html", "day.html");
                string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);

                // 3. 检查文件是否存在并加载
                // 重点修改：将 File.Exists 明确改为 System.IO.File.Exists
                if (DailyReportWebView.CoreWebView2 != null)
                {
                    // 确保 CoreWebView2 已初始化，然后订阅 NavigationCompleted 事件
                    DailyReportWebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted; // 防止重复订阅
                    DailyReportWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

                    // 使用 file:/// 协议加载本地文件 (这会触发 NavigationCompleted 事件)
                    DailyReportWebView.Source = new Uri(htmlPath);
                }
                else
                {
                    // 调试信息和错误处理
                    string errorMessage = $"<h1>错误: 找不到 day.html 文件。</h1><p>请检查文件是否位于 'assert\\html\\' 目录下，并确保其属性 '复制到输出目录' 设置为 '始终复制' 或 '如果较新则复制'。</p>";
                    DailyReportWebView.NavigateToString(errorMessage);
                }
            }
        }
        private void CoreWebView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            // 确保页面加载成功 (例如，没有出现 404 或其他错误)
            if (e.IsSuccess)
            {
                // 🚨 修正：直接调用 async void 方法，不使用 _ =
                LoadNewDashboardData();
            }
        }
        public async void LoadNewDashboardData()
        {
            var data = new DashboardData
            {
                Cards = new CardData { TotalTime = 16.5, WorkTime = 8.0, GameTime = 1.0, AfkTime = 1.5, TypingCount = 50.1, CopyCount = 12.3 },
                MixedChart = new MixedChartData
                {
                    // 示例：24小时的随机数据
                    TypingData = Enumerable.Range(0, 24).Select(i => (double)i * 0.5).ToArray(),
                    CopyData = Enumerable.Range(0, 24).Select(i => (double)i * 0.1).ToArray()
                },
                TimePie = new List<PieItem> {
            new PieItem { Value = 8.0, Name = "工作时间" },
            new PieItem { Value = 1.0, Name = "游戏时间" },
            new PieItem { Value = 1.5, Name = "AFK时间" },
            new PieItem { Value = 13.5, Name = "其他/睡眠" }
        },
                // ... AppPie 数据类似
            };

            // 1. 序列化为 JSON 字符串
            string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // 2. 构造要执行的 JavaScript 脚本
            // 注意：这里调用的是 JS 中定义的 updateDashboard(data) 函数
            string script = $"updateDashboard({jsonString});";

            // 3. 在 WebView2 中执行
            if (DailyReportWebView.CoreWebView2 != null)
            {
                // ExecuteScriptAsync 的返回是 JS 执行的结果，通常不需要用到
                await DailyReportWebView.CoreWebView2.ExecuteScriptAsync(script);
                // MessageBox.Show("数据已推送到 ECharts!"); 
            }
        }
    }
}