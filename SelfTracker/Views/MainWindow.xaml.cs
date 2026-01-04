using SelfTracker.Views;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SelfTracker
{
    public partial class MainWindow : Window
    {

        private TodayDashboardView _todayView;

        public MainWindow()
        {
            InitializeComponent();

            DataCollectors.DataCollector.Instance.Start();
            LoadBingWallpaper();
            SwitchPage("Today");
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
            {
                SwitchPage(btn.Tag.ToString());
            }
        }

        private void SwitchPage(string tag)
        {
            switch (tag)
            {
                case "Today":
                    if (_todayView == null) _todayView = new TodayDashboardView();
                    MainContentRegion.Content = _todayView;
                    break;

                case "Category":
                    MainContentRegion.Content =  new CategoryView();
                    break;

                case "History":
                    MainContentRegion.Content = new HistoryDataControl();
                    break;

                case "Settings":
                    MainContentRegion.Content = new SettingsView();
                    break;
            }
        }

        // ================= 托盘与窗口行为 =================

        private void Show_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            DataCollectors.DataCollector.Instance.Stop();
            MyNotifyIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Settings_Click(object sender, RoutedEventArgs e) => SwitchPage("Settings");
        private void OpenCategory_Click(object sender, RoutedEventArgs e) => SwitchPage("Category");
        private void OpenReport_Click(object sender, RoutedEventArgs e) => SwitchPage("History");

        private async void LoadBingWallpaper()
        {
            try
            {
                using HttpClient client = new HttpClient();
                // 必应接口：format=js(返回json), idx=0(今天), n=1(一张图)
                string jsonString = await client.GetStringAsync("https://cn.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1");

                using JsonDocument doc = JsonDocument.Parse(jsonString);
                string relativeUrl = doc.RootElement.GetProperty("images")[0].GetProperty("url").GetString();
                string wallpaperUrl = "https://www.bing.com" + relativeUrl;

                // 异步设置背景图
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(wallpaperUrl);
                bitmap.EndInit();

                WallpaperBrush.ImageSource = bitmap;
            }
            catch (Exception ex)
            {
                // 如果联网失败，可以设置一个默认颜色背景
                BackgroundGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250));
            }
        }

        #region 无边框设置
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 实现点击顶部空白区域可以拖动窗口
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            // 最小化到任务栏
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // 关闭窗口
            this.Close();
        }
        #endregion
    }
}