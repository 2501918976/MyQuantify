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
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 这个必须留着，否则 Hide 窗口后程序会直接退出
            System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            MainWindow main = new MainWindow();

            // 直接显示窗口
            main.Show();
        }
    }

}
