using Microsoft.Web.WebView2.Core;
using SelfTracker.Bridge;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace SelfTracker
{
    public partial class MainWindow : Window
    {
        // 1. 定义虚拟域名（必须与映射一致）
        private const string VirtualHostName = "selftracker.local";
        private AppBridge _appBridge;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                // 设置 WebView2 用户数据文件夹
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SelfTracker"
                );

                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);

                // 等待 CoreWebView2 初始化完成
                await MainWebView.EnsureCoreWebView2Async(env);

                // --- 解决 CORS 报错的关键步骤 ---

                // 2. 获取本地 wwwroot 的绝对路径
                string wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

                if (Directory.Exists(wwwrootPath))
                {
                    // 3. 将本地目录映射到虚拟域名
                    // 这会让 fetch('pages/today.html') 被视为在 https://selftracker.local 下的同源请求
                    MainWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        VirtualHostName,
                        wwwrootPath,
                        CoreWebView2HostResourceAccessKind.Allow
                    );

                    // 4. 使用 HTTPS 虚拟域名导航，而不是 file:// 路径
                    MainWebView.CoreWebView2.Navigate($"https://{VirtualHostName}/index.html");
                }
                else
                {
                    MessageBox.Show("找不到 wwwroot 文件夹，请检查输出目录。");
                }

                // --- 原有逻辑保留 ---

                // 创建桥接对象
                _appBridge = new AppBridge(this);
                // 注册桥接对象到 JavaScript
                MainWebView.CoreWebView2.AddHostObjectToScript("AppBridge", _appBridge);

                // 允许开发者工具
                MainWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                // 禁用右键菜单
                MainWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                // 监听事件
                MainWebView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
                MainWebView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失败: {ex.Message}");
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
                System.Diagnostics.Debug.WriteLine("页面加载成功");
            else
                System.Diagnostics.Debug.WriteLine($"页面加载失败: {e.WebErrorStatus}");
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = e.TryGetWebMessageAsString();
            System.Diagnostics.Debug.WriteLine($"来自前端的消息: {message}");
        }

        #region 窗口原生交互 (如果需要保留原先的拖动和关闭逻辑)
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Hide();
        #endregion
    }
}

