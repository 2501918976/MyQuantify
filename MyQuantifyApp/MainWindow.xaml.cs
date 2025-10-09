using MyQuantifyApp.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using WinInput = System.Windows.Input;
using SysWin = System.Windows; // 确保使用别名以避免冲突

namespace MyQuantifyApp
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // -----------------------------
        // Win32 常量和方法用于窗口缩放
        // -----------------------------

        // 边框宽度（用于触发缩放的感应区）
        private const int BorderThickness = 4;

        // 发送给 Windows 的消息 ID
        private const int WM_NCHITTEST = 0x0084;

        // 返回值定义了鼠标在窗口的哪个区域
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCAPTION = 2; // 标题栏区域，用于拖动

        // 引入 user32.dll 中的 SendMessage 函数
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


        // -----------------------------
        // 延迟隐藏导航栏 & 最小化到托盘逻辑
        // -----------------------------
        private DispatcherTimer hideNavTimer;
        // 【关键】App 实例，用于访问 _isExit 标志
        private readonly App _app;


        public MainWindow()
        {
            InitializeComponent();

            // 【关键】获取 App 实例，以便访问 _isExit 标志 (托盘最小化逻辑所需)
            _app = (App)SysWin.Application.Current;

            DataContext = new MainViewModel();

            // 初始化导航栏延迟隐藏计时器
            hideNavTimer = new DispatcherTimer();
            hideNavTimer.Interval = TimeSpan.FromSeconds(3); // 延迟 3 秒隐藏
            hideNavTimer.Tick += HideNavTimer_Tick;

            // 【注意】移除 this.StateChanged += MainWindow_StateChanged; 
            // 我们将使用覆盖的 protected override void OnStateChanged(EventArgs e) 方法来处理最小化，这样更简洁。

            // 窗口初始化时让导航面板立即显示一下，然后启动计时器隐藏
            // 【注意】NavPanel 应该是在 XAML 中定义的 Grid 或 StackPanel
            if (NavPanel != null)
            {
                var fadeIn = (Storyboard)FindResource("FadeInNav");
                Dispatcher.Invoke(() => fadeIn.Begin(NavPanel));
                hideNavTimer.Start();
            }
        }

        // -----------------------------
        // 启用无边框窗口缩放功能 (WndProc)
        // -----------------------------
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 获取窗口句柄
            IntPtr handle = new WindowInteropHelper(this).Handle;
            // 获取窗口源
            HwndSource source = HwndSource.FromHwnd(handle);
            // 添加消息处理函数
            if (source != null)
            {
                source.AddHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 拦截 WM_NCHITTEST 消息，判断鼠标是否在可缩放区域
            if (msg == WM_NCHITTEST)
            {
                // 获取鼠标相对于屏幕的坐标
                int x = (int)(lParam.ToInt64() & 0xFFFF);
                int y = (int)((lParam.ToInt64() >> 16) & 0xFFFF);

                // 获取窗口在屏幕上的位置和尺寸
                System.Windows.Point screenPoint = new System.Windows.Point(x, y);
                System.Windows.Point relativePoint = this.PointFromScreen(screenPoint);

                // 检查窗口是否处于 Normal 状态，最大化状态时不处理缩放
                if (this.WindowState == WindowState.Normal)
                {
                    // 检查是否在角落
                    if (relativePoint.Y < BorderThickness && relativePoint.X < BorderThickness)
                    {
                        handled = true;
                        return new IntPtr(HTTOPLEFT);
                    }
                    if (relativePoint.Y < BorderThickness && relativePoint.X > this.Width - BorderThickness)
                    {
                        handled = true;
                        return new IntPtr(HTTOPRIGHT);
                    }
                    if (relativePoint.Y > this.Height - BorderThickness && relativePoint.X < BorderThickness)
                    {
                        handled = true;
                        return new IntPtr(HTBOTTOMLEFT);
                    }
                    if (relativePoint.Y > this.Height - BorderThickness && relativePoint.X > this.Width - BorderThickness)
                    {
                        handled = true;
                        return new IntPtr(HTBOTTOMRIGHT);
                    }

                    // 检查是否在边框
                    if (relativePoint.X < BorderThickness)
                    {
                        handled = true;
                        return new IntPtr(HTLEFT);
                    }
                    if (relativePoint.X > this.Width - BorderThickness)
                    {
                        handled = true;
                        return new IntPtr(HTRIGHT);
                    }
                    if (relativePoint.Y < BorderThickness)
                    {
                        handled = true;
                        return new IntPtr(HTTOP);
                    }
                    if (relativePoint.Y > this.Height - BorderThickness)
                    {
                        handled = true;
                        return new IntPtr(HTBOTTOM);
                    }
                }
            }
            return IntPtr.Zero;
        }

        // -----------------------------
        // 导航栏延迟隐藏处理
        // -----------------------------
        private void HideNavTimer_Tick(object sender, EventArgs e)
        {
            hideNavTimer.Stop();
            var fadeOut = (Storyboard)FindResource("FadeOutNav");
            if (NavPanel != null)
            {
                fadeOut.Begin(NavPanel);
            }
        }

        private void TitleBarGrid_MouseEnter(object sender, WinInput.MouseEventArgs e)
        {
            hideNavTimer.Stop(); // 停掉计时器
            var fadeIn = (Storyboard)FindResource("FadeInNav");
            if (NavPanel != null)
            {
                fadeIn.Begin(NavPanel);
            }
        }

        private void TitleBarGrid_MouseLeave(object sender, WinInput.MouseEventArgs e)
        {
            hideNavTimer.Stop();
            hideNavTimer.Start(); // 鼠标移开后启动计时器
        }

        // -----------------------------
        // 窗口拖动
        // -----------------------------
        // 移除了不必要的字段，DragMove() 已经足够
        private void TitleBarGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 确保点击的不是 Button 控件，避免拖动与点击事件冲突
                if (!(e.OriginalSource is System.Windows.Controls.Button))
                {
                    this.DragMove();
                    e.Handled = true;
                }
            }
        }

        // -----------------------------
        // 窗口控制按钮
        // -----------------------------
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // 点击最小化按钮时，会触发 OnStateChanged 逻辑进行隐藏
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 点击关闭按钮时，会触发 OnClosing 逻辑进行拦截和隐藏
            this.Close();
        }

        // -----------------------------
        // 【关键】最小化到托盘功能实现
        // -----------------------------

        /// <summary>
        /// 覆盖窗口状态改变事件。用于在最小化时隐藏窗口，实现最小化到托盘效果。
        /// </summary>
        protected override void OnStateChanged(EventArgs e)
        {
            // 当窗口被最小化时 (例如点击了窗口的最小化按钮)
            if (this.WindowState == SysWin.WindowState.Minimized)
            {
                this.Hide(); // 隐藏窗口，避免它出现在任务栏，仅保留托盘图标
            }

            base.OnStateChanged(e);
        }

        /// <summary>
        /// 覆盖窗口关闭事件。用于拦截关闭操作，将其转换为最小化到托盘。
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            // 检查 App 中的退出标志 (_isExit)。
            // 如果 _isExit 为 false，表示用户点击了关闭按钮或按下 Alt+F4，此时我们应取消关闭，并隐藏窗口。
            if (!_app._isExit)
            {
                e.Cancel = true; // 取消关闭操作
                this.Hide();    // 隐藏窗口，让程序继续在后台运行 (图标显示在系统托盘)
            }
            else
            {
                // 如果 _isExit 为 true (通过托盘菜单的“退出应用”触发)，则允许窗口正常关闭。
                base.OnClosing(e);
            }
        }
    }
}
