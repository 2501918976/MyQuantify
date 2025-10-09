using Microsoft.VisualBasic.Logging;
using MyQuantifyApp.Database;
using MyQuantifyApp.Database.Repositories.Raw;
using MyQuantifyApp.Service;
using MyQuantifyApp.Service.Services;
using MyQuantifyApp.Services;
using MyQuantifyApp.ViewModels;
using Serilog;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SysWin = System.Windows;
using Microsoft.Extensions.Configuration;


namespace MyQuantifyApp
{

    public partial class App : SysWin.Application
    {
        private NotifyIcon _notifyIcon;
        public bool _isExit;
        private const int AGGREGATION_INTERVAL_MINUTES = 1;
        public static MainViewModel MainVmInstance { get; set; }
        private readonly SQLiteDataService _dataService = new SQLiteDataService();
        private ActivityMonitorService _monitorService;
        private DataFlushService _flushService;
        private AggregationService _aggregationService;
        private CancellationTokenSource _aggregationCts;

        protected override void OnStartup(SysWin.StartupEventArgs e)
        {
            // ────────────────────────────────
            // 1️⃣ 初始化日志系统
            // ────────────────────────────────
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File(@"C:\Users\admin\source\repos\MyQuantify\MyQuantifyApp\Logs\applog-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            base.OnStartup(e);

            this.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

            // 2️⃣ 数据库初始化
            _dataService.InitializeDatabase();
            string connString = _dataService.ConnectionString;


            // 3️⃣ 仓储层初始化 (Repositories)
            var processRepo = new ProcessRepository(connString);
            var windowRepo = new WindowRepository(connString);
            var windowActivityRepo = new WindowActivityRepository(connString);
            var keyRepo = new KeyCharDataRepository(connString);
            var clipboardRepo = new ClipboardActivityDataRepository(connString);
            var afkRepo = new AfkActivityDataRepository(connString);

            // 4️⃣ 服务层初始化 (Services)
            _monitorService = new ActivityMonitorService(
                processRepo,
                windowRepo,
                windowActivityRepo
            );

            _flushService = new DataFlushService(
                _monitorService,
                keyRepo,
                windowRepo,
                windowActivityRepo,
                clipboardRepo,
                afkRepo
            );

            _aggregationService = new AggregationService(connString);

            _monitorService.SetDataFlushService(_flushService);

            // 5️⃣ 启动监控服务
            try
            {
                _monitorService.StartMonitoring();

                StartAggregationTask();
            }
            catch (Win32Exception ex)
            {
                //Serilog.Log.Fatal(ex, "致命错误：钩子安装失败。请尝试以管理员身份运行。错误码: {ErrorCode}", ex.NativeErrorCode);
                SysWin.MessageBox.Show(
                    $"致命错误：钩子安装失败。请以管理员身份运行。\n错误码: {ex.NativeErrorCode}",
                    "监控启动失败",
                    SysWin.MessageBoxButton.OK, SysWin.MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }
            catch (Exception ex)
            {
                //Serilog.Log.Fatal(ex, "启动监控时发生未知错误: {Message}", ex.Message);
                SysWin.MessageBox.Show(
                    $"启动监控时发生未知错误: {ex.Message}",
                    "监控启动失败",
                    SysWin.MessageBoxButton.OK, SysWin.MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            // 6️⃣ 最小化到托盘初始化
            InitializeNotifyIcon();

            // 必须在所有初始化完成后显示主窗口
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        /// <summary>
        /// 初始化系统托盘图标（安全版，带默认图标容错）。
        /// </summary>
        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "MyQuantifyApp - 生产力追踪";

            try
            {
                var iconStream = GetType().Assembly.GetManifestResourceStream("MyQuantifyApp.assets.logo.slpsv-h5a2m-001.ico");
                if (iconStream != null)
                {
                    _notifyIcon.Icon = new Icon(iconStream);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            _notifyIcon.MouseDoubleClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowMainWindow();
                }
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("显示主界面", null, (s, e) => ShowMainWindow());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出应用", null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = menu;
        }

        /// <summary>
        /// 托盘图标双击事件，用于显示主窗口。
        /// </summary>
        private void NotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        }

        /// <summary>
        /// 显示或恢复主窗口
        /// </summary>
        private void ShowMainWindow()
        {
            if (MainWindow == null) return;

            if (!MainWindow.IsVisible)
            {
                MainWindow.Show();
            }

            if (MainWindow.WindowState == SysWin.WindowState.Minimized)
            {
                MainWindow.WindowState = SysWin.WindowState.Normal;
            }

            MainWindow.Activate();
        }

        /// <summary>
        /// 退出应用
        /// </summary>
        private void ExitApplication()
        {
            _isExit = true;
            MainWindow?.Close();
        }

        /// <summary>
        /// 启动后台任务，每 {AGGREGATION_INTERVAL_MINUTES} 分钟执行一次数据聚合。
        /// </summary>
        private void StartAggregationTask()
        {
            _aggregationCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                var token = _aggregationCts.Token;

                // 首次启动时立即聚合一次
                PerformAggregation();

                while (!token.IsCancellationRequested)
                {
                    // ⚠️ 修改：使用常量 AGGREGATION_INTERVAL_MINUTES 来设置延迟时间
                    await Task.Delay(TimeSpan.FromMinutes(AGGREGATION_INTERVAL_MINUTES), token);

                    if (token.IsCancellationRequested)
                        break;

                    PerformAggregation();
                }
            }, _aggregationCts.Token);
        }

        /// <summary>
        /// 执行数据聚合操作，只聚合当天的数据。
        /// </summary>
        private void PerformAggregation()
        {
            try
            {
                //Serilog.Log.Debug("⏱️ 开始执行每日数据聚合...");
                _aggregationService.AggregateAll(DateTime.Today);
                //Serilog.Log.Debug("✅ 数据聚合完成。");
            }
            catch (Exception ex)
            {
                //Serilog.Log.Error(ex, "数据聚合任务失败。");
            }
        }

        protected override void OnExit(SysWin.ExitEventArgs e)
        {
            //Serilog.Log.Information("🔴 应用程序退出，执行清理任务...");

            // 停止后台聚合任务
            _aggregationCts?.Cancel();
            //Serilog.Log.Information("🔴 停止后台聚合任务...");

            // 关键：释放托盘图标资源
            if (_notifyIcon != null)
            {
                _notifyIcon.Dispose();
            }

            try
            {
                _monitorService?.CheckAfkStatus();
                _monitorService?.FlushCurrentActiveWindow();
                _monitorService?.StopMonitoring();
            }
            catch (Exception ex)
            {
                //Serilog.Log.Error(ex, "退出时清理失败");
            }

            //Serilog.Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
