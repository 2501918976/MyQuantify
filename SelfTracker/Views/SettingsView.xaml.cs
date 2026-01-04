using Microsoft.Win32;
using SelfTracker.DataCollectors;
using SelfTracker.Setting;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SelfTracker.Views
{

    public partial class SettingsView : System.Windows.Controls.UserControl
    {
        #region 构造函数

        public SettingsView()
        {
            InitializeComponent();
        }

        #endregion

        #region 初始化和加载

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCurrentSettings();
        }

        /// <summary>
        /// 加载当前设置到界面
        /// </summary>
        private void LoadCurrentSettings()
        {
            var config = AppSettings.Load();

            // 数据采集设置
            SliderLogInterval.Value = config.LogIntervalSeconds;
            SliderAFKTimeout.Value = config.AFKTimeoutSeconds;

            // 外观设置 - 透明度
            SliderOpacity.Value = config.BackgroundOpacity;

            // 外观设置 - 主题颜色
            foreach (ComboBoxItem item in ComboTheme.Items)
            {
                if (item.Tag.ToString() == config.ThemeColor)
                {
                    ComboTheme.SelectedItem = item;
                    break;
                }
            }
            // 加载字体颜色
            foreach (ComboBoxItem item in ComboTextColor.Items)
            {
                if (item.Tag.ToString() == config.TextColor)
                {
                    ComboTextColor.SelectedItem = item;
                    break;
                }
            }
            // 外观设置 - 背景图片
            switch (config.BackgroundMode)
            {
                case "None":
                    RbNoBackground.IsChecked = true;
                    break;
                case "Bing":
                    RbBingBackground.IsChecked = true;
                    break;
                case "Custom":
                    RbCustomBackground.IsChecked = true;
                    TxtCustomImagePath.Text = config.CustomBackgroundPath;
                    break;
            }

            // 系统设置 - 自动启动
            ChkAutoStart.IsChecked = config.AutoStart;

            // 更新显示值
            UpdateLogIntervalDisplay();
            UpdateAFKTimeoutDisplay();
            UpdateOpacityDisplay();
        }

        #endregion

        #region 滑块值变化事件

        private void SliderLogInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateLogIntervalDisplay();
        }

        private void SliderAFKTimeout_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateAFKTimeoutDisplay();
        }

        private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateOpacityDisplay();
        }

        #endregion

        #region 显示更新方法

        /// <summary>
        /// 更新数据入库频率显示
        /// </summary>
        private void UpdateLogIntervalDisplay()
        {
            if (TxtLogIntervalValue == null) return;

            int seconds = (int)SliderLogInterval.Value;
            TxtLogIntervalValue.Text = FormatTimeValue(seconds);
        }

        /// <summary>
        /// 更新 AFK 超时时间显示
        /// </summary>
        private void UpdateAFKTimeoutDisplay()
        {
            if (TxtAFKTimeoutValue == null) return;

            int seconds = (int)SliderAFKTimeout.Value;
            TxtAFKTimeoutValue.Text = FormatTimeValue(seconds);
        }

        /// <summary>
        /// 更新透明度百分比显示
        /// </summary>
        private void UpdateOpacityDisplay()
        {
            if (TxtOpacityValue == null) return;

            int percentage = (int)(SliderOpacity.Value * 100);
            TxtOpacityValue.Text = $"{percentage}%";
        }

        /// <summary>
        /// 格式化时间值为友好的显示文本
        /// </summary>
        /// <param name="seconds">秒数</param>
        /// <returns>格式化后的时间字符串</returns>
        private string FormatTimeValue(int seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds} 秒";
            }
            else if (seconds % 60 == 0)
            {
                return $"{seconds / 60} 分钟";
            }
            else
            {
                int minutes = seconds / 60;
                int remainingSeconds = seconds % 60;
                return $"{minutes} 分 {remainingSeconds} 秒";
            }
        }

        #endregion

        #region 背景图片相关

        /// <summary>
        /// Bing 壁纸单选按钮被选中
        /// </summary>
        private void RbBingBackground_Checked(object sender, RoutedEventArgs e)
        {
            if (TxtCustomImagePath != null)
            {
                TxtCustomImagePath.Clear();
            }
        }

        /// <summary>
        /// 自定义背景单选按钮被选中
        /// </summary>
        private void RbCustomBackground_Checked(object sender, RoutedEventArgs e)
        {
            // 自定义背景被选中时的处理逻辑
            // 可以在这里添加提示用户选择图片的逻辑
        }

        /// <summary>
        /// 浏览并选择背景图片
        /// </summary>
        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择背景图片",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                TxtCustomImagePath.Text = dialog.FileName;
                RbCustomBackground.IsChecked = true;
            }
        }

        #endregion

        #region 保存和重置

        /// <summary>
        /// 保存所有设置
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 确定背景模式
                string backgroundMode = "None";
                string customPath = null;

                if (RbBingBackground.IsChecked == true)
                {
                    backgroundMode = "Bing";
                }
                else if (RbCustomBackground.IsChecked == true)
                {
                    backgroundMode = "Custom";
                    customPath = TxtCustomImagePath.Text.Trim();

                    if (string.IsNullOrEmpty(customPath) || !File.Exists(customPath))
                    {
                        System.Windows.MessageBox.Show("请选择有效的背景图片文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // 创建配置对象
                var config = new AppSettings
                {
                    LogIntervalSeconds = (int)SliderLogInterval.Value,
                    AFKTimeoutSeconds = (int)SliderAFKTimeout.Value,
                    ThemeColor = (ComboTheme.SelectedItem as ComboBoxItem)?.Tag.ToString(),
                    TextColor = (ComboTextColor.SelectedItem as ComboBoxItem)?.Tag.ToString(), // 获取选中的字体颜色
                    BackgroundOpacity = SliderOpacity.Value,
                    BackgroundMode = backgroundMode,
                    CustomBackgroundPath = customPath,
                    AutoStart = ChkAutoStart.IsChecked ?? false
                };

                // 保存到磁盘
                config.Save();

                // 通知数据采集服务更新参数
                DataCollector.Instance?.RefreshSettings();

                // 应用自启动设置
                if (config.AutoStart)
                    AutoStartManager.Enable();
                else
                    AutoStartManager.Disable();

                // 应用主题/字体颜色
                ApplyThemeColor(config.ThemeColor);
                ApplyTextColor(config.TextColor, config.ThemeColor);
                // 应用背景设置
                ApplyBackgroundSettings(config);

                System.Windows.MessageBox.Show("设置已保存并立即生效！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存设置时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重置所有设置为默认值
        /// </summary>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "确定要重置所有设置为默认值吗？",
                "确认重置",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 创建默认配置
                var defaultConfig = new AppSettings
                {
                    LogIntervalSeconds = 30,
                    AFKTimeoutSeconds = 180,
                    ThemeColor = "#4E73DF",
                    BackgroundOpacity = 0.95,
                    BackgroundMode = "None",
                    CustomBackgroundPath = null,
                    AutoStart = false
                };

                // 保存默认配置
                defaultConfig.Save();

                // 重新加载界面
                LoadCurrentSettings();

                // 应用设置
                DataCollector.Instance?.RefreshSettings();
                ApplyThemeColor(defaultConfig.ThemeColor);
                ApplyBackgroundSettings(defaultConfig);

                System.Windows.MessageBox.Show("设置已重置为默认值", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region 应用设置方法

        /// <summary>
        /// 应用主题颜色到全局资源
        /// </summary>
        /// <param name="colorCode">颜色代码（如 #4E73DF）</param>
        private void ApplyThemeColor(string colorCode)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorCode);
                System.Windows.Application.Current.Resources["PrimaryColorBrush"] = new SolidColorBrush(color);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题颜色失败: {ex.Message}");
            }
        }

        private void ApplyTextColor(string textColorCode, string themeColorCode)
        {
            try
            {
                string finalColor = textColorCode;

                // 处理“自动”逻辑：根据主题颜色深浅决定用黑还是白
                if (textColorCode == "Auto")
                {
                    var themeColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(themeColorCode);
                    // 亮度计算公式
                    double brightness = (0.299 * themeColor.R + 0.587 * themeColor.G + 0.114 * themeColor.B) / 255;
                    finalColor = brightness < 0.5 ? "#FFFFFF" : "#2C3E50";
                }

                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(finalColor);
                // 更新全局资源
                System.Windows.Application.Current.Resources["GlobalFontBrush"] = new SolidColorBrush(color);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用字体颜色失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用背景设置到主窗口
        /// </summary>
        /// <param name="config">应用设置对象</param>
        private void ApplyBackgroundSettings(AppSettings config)
        {
            try
            {
                Window mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow == null) return;

                // 设置窗口透明度
                mainWindow.Opacity = config.BackgroundOpacity;

                // 根据模式设置背景
                switch (config.BackgroundMode)
                {
                    case "None":
                        // 无背景 - 使用纯白色
                        mainWindow.Background = new SolidColorBrush(Colors.White);
                        break;

                    case "Bing":
                        // 使用 Bing 每日壁纸
                        LoadBingWallpaperAsync(mainWindow);
                        break;

                    case "Custom":
                        // 使用自定义图片
                        if (!string.IsNullOrEmpty(config.CustomBackgroundPath) && File.Exists(config.CustomBackgroundPath))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(config.CustomBackgroundPath, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();

                            ImageBrush imageBrush = new ImageBrush(bitmap)
                            {
                                Stretch = Stretch.UniformToFill
                            };
                            mainWindow.Background = imageBrush;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用背景设置失败: {ex.Message}");
            }
        }

        #endregion

        #region Bing 壁纸加载

        /// <summary>
        /// 异步加载 Bing 每日壁纸
        /// </summary>
        /// <param name="window">目标窗口</param>
        private async void LoadBingWallpaperAsync(Window window)
        {
            try
            {
                string imageUrl = await GetBingWallpaperUrlAsync();
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImageBrush imageBrush = new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.UniformToFill
                    };

                    window.Background = imageBrush;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载 Bing 壁纸失败: {ex.Message}");
                // 加载失败时使用白色背景
                window.Background = new SolidColorBrush(Colors.White);
            }
        }

        /// <summary>
        /// 获取 Bing 每日壁纸 URL
        /// </summary>
        /// <returns>壁纸的完整 URL</returns>
        private async Task<string> GetBingWallpaperUrlAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string apiUrl = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=zh-CN";
                    string json = await client.GetStringAsync(apiUrl);

                    // 简单解析 JSON（实际项目建议使用 System.Text.Json 或 Newtonsoft.Json）
                    int urlStartIndex = json.IndexOf("\"url\":\"") + 7;
                    int urlEndIndex = json.IndexOf("\"", urlStartIndex);
                    string relativeUrl = json.Substring(urlStartIndex, urlEndIndex - urlStartIndex);

                    return "https://www.bing.com" + relativeUrl;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取 Bing 壁纸 URL 失败: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
