using System.Configuration;
using System.Data;
using System.Windows;

namespace SelfTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        // App.xaml.cs
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // --- 关键点：启动数据采集器 ---
            // 这会触发 DataCollector 的构造函数，进而初始化数据库
            SelfTracker.DataCollectors.DataCollector.Instance.Start();

            MainWindow main = new MainWindow();
            main.Show();
        }
    }

}
