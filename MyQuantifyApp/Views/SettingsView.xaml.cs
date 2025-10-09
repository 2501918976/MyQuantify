using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public partial class SettingsView : Page
    {
        public SettingsView()
        {
            InitializeComponent();
            this.Loaded += SettingWebView_Loaded;
        }

        private async void SettingWebView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SettingWebView != null)
            {
                await SettingWebView.EnsureCoreWebView2Async();

                string subPath = System.IO.Path.Combine("wwwroot", "Settings.html");
                string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);

                if (SettingWebView.CoreWebView2 != null)
                {
                    SettingWebView.Source = new Uri(htmlPath);
                    SettingWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                }
                else
                {
                    SettingWebView.NavigateToString("<h1>错误: 找不到 Settings.html 文件。</h1>");
                }
            }
        }
    }
}