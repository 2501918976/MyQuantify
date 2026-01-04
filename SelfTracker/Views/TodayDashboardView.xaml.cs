using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SelfTracker.Repository;

namespace SelfTracker.Views
{
    public partial class TodayDashboardView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private readonly DataRepository _repo;
        private readonly DispatcherTimer _refreshTimer;

        public TodayDashboardView()
        {
            InitializeComponent();

            // 将 DataContext 指向自身，以便 XAML 绑定到下方的属性
            this.DataContext = this;

            // 初始化数据库访问
            var dbService = new SQLiteDataService();
            _repo = new DataRepository(dbService.ConnectionString);

            // UI 刷新定时器
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _refreshTimer.Tick += (s, e) => RefreshDataFromDb();
            _refreshTimer.Start();

            // 初始加载
            RefreshDataFromDb();
        }

        #region 绑定属性 (原 MainViewModel 内容)

        private string _currentProcess;
        public string CurrentProcess
        {
            get => _currentProcess;
            set { _currentProcess = value; OnPropertyChanged(); }
        }

        private string _currentWindow;
        public string CurrentWindow
        {
            get => _currentWindow;
            set { _currentWindow = value; OnPropertyChanged(); }
        }

        private int _keyboardCount;
        public string KeyboardCountText => $"{_keyboardCount}";

        private int _clipboardCount;
        public string ClipboardCountText => $"{_clipboardCount}";

        private TimeSpan _totalActiveTime = TimeSpan.Zero;
        public string TodayActiveTimeText =>
            $"{(int)_totalActiveTime.TotalHours:D2}:{_totalActiveTime.Minutes:D2}:{_totalActiveTime.Seconds:D2}";

        private TimeSpan _totalAFKTime = TimeSpan.Zero;
        public string TodayAFKTimeText =>
            $"{(int)_totalAFKTime.TotalHours:D2}:{_totalAFKTime.Minutes:D2}:{_totalAFKTime.Seconds:D2}";

        #endregion

        private void RefreshNow_Click(object sender, RoutedEventArgs e)
        {
            // 强制写入并刷新
            DataCollectors.DataCollector.Instance.ForceLogToDb();
            RefreshDataFromDb();
        }

        private void RefreshDataFromDb()
        {
            try
            {
                // 从数据库读取
                int totalKeys = _repo.GetTodayTotalKeystrokes();
                int totalCopies = _repo.GetTodayTotalCopyCount();
                int activeSeconds = _repo.GetTodayActiveDurationSeconds();
                int afkSeconds = _repo.GetTodayAfkDurationSeconds();
                var latestActivity = _repo.GetLatestActivity();

                // 更新内部字段并触发通知
                _keyboardCount = totalKeys;
                _clipboardCount = totalCopies;
                _totalActiveTime = TimeSpan.FromSeconds(activeSeconds);
                _totalAFKTime = TimeSpan.FromSeconds(afkSeconds);

                if (latestActivity != null)
                {
                    CurrentProcess = latestActivity.ProcessName == "Idle" ? "状态：离开 (AFK)" : $"当前应用：{latestActivity.ProcessName}";
                    CurrentWindow = latestActivity.WindowTitle;
                }

                // 显式触发汇总文本的更新通知
                OnPropertyChanged(nameof(KeyboardCountText));
                OnPropertyChanged(nameof(ClipboardCountText));
                OnPropertyChanged(nameof(TodayActiveTimeText));
                OnPropertyChanged(nameof(TodayAFKTimeText));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新数据失败: {ex.Message}");
            }
        }

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}