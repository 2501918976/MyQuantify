using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Drawing;

namespace MyQuantifyApp.Views
{
    public partial class FocusView : Page
    {

        public FocusView()
        {
            InitializeComponent();
            this.Loaded += FocusWebView_Loaded;
        }

        private async void FocusWebView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (FocusWebView != null)
            {
                await FocusWebView.EnsureCoreWebView2Async();

                string subPath = System.IO.Path.Combine("wwwroot", "Focus.html");
                string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subPath);

                if (FocusWebView.CoreWebView2 != null)
                {
                    FocusWebView.Source = new Uri(htmlPath);
                    FocusWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                }
                else
                {
                    FocusWebView.NavigateToString("<h1>错误: 找不到 Focus.html 文件。</h1>");
                }
            }
        }
    }
}