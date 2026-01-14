using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace SelfTracker
{
    public partial class MainWindow : Window
    {
        // 定义虚拟域名
        private const string VirtualHostName = "selftracker.local";

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                // 1. 等待 WebView2 运行时环境就绪
                await MainWebView.EnsureCoreWebView2Async(null);

                // 2. 获取 wwwroot 的绝对路径
                string wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

                if (Directory.Exists(wwwrootPath))
                {
                    // 3. 核心步骤：将虚拟域名映射到本地文件夹
                    // 这样访问 https://selftracker.local/ 就等于访问本地 wwwroot 目录
                    MainWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        VirtualHostName,
                        wwwrootPath,
                        CoreWebView2HostResourceAccessKind.Allow
                    );

                    // 4. 通过 HTTPS 协议加载页面，彻底解决 CORS 限制
                    MainWebView.Source = new Uri($"https://{VirtualHostName}/index.html");
                }
                else
                {
                    MainWebView.NavigateToString("<h1 style='color:red; background:white; padding:20px;'>" +
                        "Web 资源缺失<br><small>请确保程序根目录下存在 wwwroot 文件夹</small></h1>");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 初始化失败: {ex.Message}\n请确认已安装 WebView2 Runtime。", "系统错误");
            }
        }

        #region 窗口原生交互

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        // 如果你希望点击关闭后彻底退出，将 Hide() 改为 Application.Current.Shutdown();
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Hide();

        #endregion
    }
}