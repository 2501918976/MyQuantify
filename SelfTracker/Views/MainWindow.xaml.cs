using Microsoft.Web.WebView2.Core;
using SelfTracker.DataCollectors;
using SelfTracker.wwwroot;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace SelfTracker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {

                await MainWebView.EnsureCoreWebView2Async(null);
                MainWebView.CoreWebView2.AddHostObjectToScript("bridge", new Bridge());

                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
                if (File.Exists(htmlPath))
                {
                    MainWebView.Source = new Uri(htmlPath);
                }
                else
                {
                    MainWebView.NavigateToString("<h1 style='color:red'>Web 资源缺失</h1>");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"WebView2 初始化失败: {ex.Message}");
            }
        }

        #region 窗口原生交互

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Hide();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        #endregion

    }
}