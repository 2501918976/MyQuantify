using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace SelfTracker.Views
{
    public partial class HistoryDataControl : System.Windows.Controls.UserControl
    {
        public HistoryDataControl()
        {
            InitializeComponent();
            // 初始化 WebView2
            InitializeWebViewAsync();
        }

        private async void InitializeWebViewAsync()
        {
            try
            {
                // 1. 等待 CoreWebView2 环境初始化
                await HistoryWebView.EnsureCoreWebView2Async(null);

                // 禁止显示右键菜单 (可选，让它更像原生应用)
                HistoryWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                // 2. 确定 HTML 路径
                string filePath = @"C:\Users\admin\source\repos\SelfTracker\SelfTracker\wwwroot\history_data.html";

                if (File.Exists(filePath))
                {
                    // 3. 导航到页面
                    HistoryWebView.CoreWebView2.Navigate(filePath);
                }
                else
                {
                    string errorHtml = $"<html><body style='background:#f8f9fa;'><h1>未找到文件</h1><p>{filePath}</p></body></html>";
                    HistoryWebView.NavigateToString(errorHtml);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("WebView2 初始化失败，请检查是否安装了 Edge 浏览器运行时。\n错误: " + ex.Message);
            }
        }
    }
}