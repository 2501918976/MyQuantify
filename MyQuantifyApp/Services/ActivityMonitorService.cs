using MyQuantifyApp.Database.Models;
using MyQuantifyApp.Service.Services;
using MyQuantifyApp.Services.Other;
using Serilog;
using System.Windows;
using MyQuantifyApp.Services.Basic;
using MyQuantifyApp.Services;
using MyQuantifyApp.Database.Repositories.Raw;

namespace MyQuantifyApp.Service
{
    public class ActivityMonitorService
    {
        // ====================================================================
        // 1. 依赖项 (Dependency Injection)
        // ====================================================================

        private DataFlushService _dataFlushService;
        private readonly ProcessRepository _processRepository;
        private readonly WindowRepository _windowRepository;
        private readonly WindowActivityRepository _windowActivityRepository;

        // ====================================================================
        // 2. 字段 (Hooks, AFK State, Buffers)
        // ====================================================================

        private ActiveWindowHook _activeWindowHook;
        private ClipboardMonitor _clipboardMonitor;
        private KeyboardHook _keyboardHook;

        private const int BATCH_SIZE_THRESHOLD = 50;
        private const double BATCH_INTERVAL_MS = 5000;

        private const double AFK_TIMEOUT_MS = 2 * 60 * 1000; // 2分钟AFK阈值
        private DateTime _lastActivityTimestamp;
        private bool _isUserAfk = false;
        private DateTime _afkStartTime = default;

        public readonly List<KeyCharData> _keyCharBuffer = new List<KeyCharData>();
        public readonly List<WindowActivityData> _windowBuffer = new List<WindowActivityData>();
        public readonly List<ClipboardActivityData> _clipboardBuffer = new List<ClipboardActivityData>();
        public readonly List<AfkData> _afkBuffer = new List<AfkData>();

        // 用于跟踪当前正在计时的窗口活动会话
        private WindowActivityData _currentWindowActivity;

        // ====================================================================
        // 3. 构造函数
        // ====================================================================

        public ActivityMonitorService(ProcessRepository processRepository, WindowRepository windowRepository, WindowActivityRepository windowActivityRepository)
        {
            _processRepository = processRepository;
            _windowRepository = windowRepository;
            _windowActivityRepository = windowActivityRepository;

            _activeWindowHook = new ActiveWindowHook();
            _clipboardMonitor = new ClipboardMonitor();
            _keyboardHook = new KeyboardHook();
            _lastActivityTimestamp = DateTime.Now;
        }

        public void SetDataFlushService(DataFlushService dataFlushService)
        {
            _dataFlushService = dataFlushService;
        }

        // ====================================================================
        // 4. 监控生命周期 (Monitoring Lifecycle)
        // ====================================================================

        public void StartMonitoring()
        {
            // 订阅事件
            _activeWindowHook.ActiveWindowChanged += OnActiveWindowChanged;
            _clipboardMonitor.ClipboardContentChanged += OnClipboardContentChanged;
            _keyboardHook.StringDown += OnKeyStringDown;

            try
            {
                _activeWindowHook.Hook();
                _clipboardMonitor.Start();
                _keyboardHook.Hook();
                //Log.Information("活动监控钩子已启动.");

                if (_dataFlushService == null)
                {
                    //Log.Fatal("DataFlushService 尚未通过 SetDataFlushService 注入。服务启动失败。");
                    throw new InvalidOperationException("DataFlushService must be set before starting monitoring.");
                }
                _dataFlushService.StartFlushing();
            }
            catch (Exception ex)
            {
                //Log.Fatal(ex, "启动监控时发生致命错误。");
                System.Windows.MessageBox.Show($"致命错误：监控启动失败。", "监控启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StopMonitoring()
        {
            // 【AFK 退出处理】如果退出时处于AFK状态，记录AFK结束
            if (_isUserAfk)
            {
                LogAfkEnd();
            }

            // 确保最后一个窗口活动会话被正确记录
            FlushCurrentActiveWindowLogic();

            // 停止 DataFlushService 的定时器并执行最后一次刷新
            _dataFlushService?.StopFlushing();

            _activeWindowHook?.UnHook();
            _clipboardMonitor?.Stop();
            _keyboardHook?.UnHook();
            (_clipboardMonitor as IDisposable)?.Dispose();

            //Log.Information("监控服务已停止，钩子已解除。");
        }

        // ====================================================================
        // 5. 状态管理 (Activity & AFK)
        // ====================================================================

        private void RecordActivity()
        {
            _lastActivityTimestamp = DateTime.Now;

            if (_isUserAfk)
            {
                LogAfkEnd();
                _isUserAfk = false;
            }
        }

        public void CheckAfkStatus()
        {
            if (!_isUserAfk && (DateTime.Now - _lastActivityTimestamp).TotalMilliseconds >= AFK_TIMEOUT_MS)
            {
                // ❗ 改进：AFK 开始前，结束当前窗口会话
                FlushCurrentActiveWindowLogic();

                _isUserAfk = true;
                LogAfkStart();
            }
        }

        private void LogAfkStart()
        {
            _afkStartTime = DateTime.Now;
            //Log.Warning("用户进入 AFK 状态，开始时间: {Time}", _afkStartTime);
        }

        private void LogAfkEnd()
        {
            if (_afkStartTime != default)
            {
                DateTime afkEndTime = DateTime.Now;
                var duration = afkEndTime - _afkStartTime;

                if (duration.TotalSeconds >= 5)
                {
                    var afkData = new AfkData
                    {
                        StartTime = _afkStartTime,
                        EndTime = afkEndTime,
                        DurationSeconds = (int)duration.TotalSeconds
                    };
                    lock (_afkBuffer)
                    {
                        _afkBuffer.Add(afkData);
                    }
                    //Log.Warning("用户从 AFK 状态返回。AFK 持续时间: {Duration} 秒", afkData.DurationSeconds);
                }
            }
            _afkStartTime = default;
        }

        // ====================================================================
        // 6. 事件处理 (Event Handlers)
        // ====================================================================

        private void OnActiveWindowChanged(object sender, ActiveWindowChangedEventArgs e)
        {
            RecordActivity();

            // 1️⃣ 结束上一个窗口会话
            FlushCurrentActiveWindowLogic();

            try
            {
                // 2️⃣ 查找或创建 Process
                // 使用新的 FindOrCreateProcess 方法，它接受 ProcessName 和 FilePath
                var process = _processRepository.FindOrCreateProcess(
                    e.ProcessName,
                    e.FilePath
                );

                // 3️⃣ 查找或创建 Window (假设 _windowRepository 也有 FindOrCreateWindow 方法)
                // 注意：ProcessInfo.CategoryId 是可空的 (int?)，可以直接传递
                var window = _windowRepository.FindOrCreateWindow(
                    process.Id,                  // 第 1 个参数：ProcessId
                    e.Title,                     // 第 2 个参数：WindowTitle
                    process.CategoryId           // ✅ 第 3 个参数：从 ProcessInfo 获取 CategoryId (int?) 并传入
                );

                // 4️⃣ 启动新的窗口会话
                _currentWindowActivity = new WindowActivityData
                {
                    WindowId = window.Id,
                    StartTime = DateTime.Now
                };

                //Log.Information("🪟 窗口切换：{Title} ({Process})", e.Title, e.ProcessName);
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "创建窗口活动记录失败。进程: {ProcessName}, 窗口: {Title}", e.ProcessName, e.Title);
            }
        }

        private void OnClipboardContentChanged(object sender, string content)
        {
            RecordActivity();
            //Log.Debug("剪贴板内容变化: 内容长度 {Length}", content.Length);

            var clipboardData = new ClipboardActivityData
            {
                Content = content,
                Length = content.Length,
                Timestamp = DateTime.Now
            };

            lock (_clipboardBuffer)
            {
                _clipboardBuffer.Add(clipboardData);
            }
        }
        private void OnKeyStringDown(object sender, StringDownEventArgs e)
        {
            if (e.IsChar)
            {
                RecordActivity();

                var keyData = new KeyCharData
                {
                    KeyChar = e.Value,
                    Timestamp = DateTime.Now
                };

                lock (_keyCharBuffer)
                {
                    _keyCharBuffer.Add(keyData);

                    if (_keyCharBuffer.Count >= BATCH_SIZE_THRESHOLD)
                    {
                        //Log.Debug("按键数据达到阈值 {Threshold}，触发立即批量写入.", BATCH_SIZE_THRESHOLD);
                        System.Threading.Tasks.Task.Run(() => _dataFlushService.FlushKeyBuffer());
                    }
                }
            }
        }

        // ====================================================================
        // 7. 缓冲区逻辑 (Buffer Logic - 仅处理业务逻辑，不涉及写入)
        // ====================================================================

        public void FlushCurrentActiveWindow()
        {
            FlushCurrentActiveWindowLogic();
        }

        private void FlushCurrentActiveWindowLogic()
        {
            if (_currentWindowActivity == null)
                return;

            _currentWindowActivity.EndTime = DateTime.Now;
            _currentWindowActivity.DurationSeconds =
                (int)(_currentWindowActivity.EndTime - _currentWindowActivity.StartTime).TotalSeconds;

            if (_currentWindowActivity.DurationSeconds >= 1)
            {
                lock (_windowBuffer)
                {
                    _windowBuffer.Add(_currentWindowActivity);
                }

                //Log.Debug("✅ 记录窗口活动：{Duration}s - WindowId={Id}",
                //    _currentWindowActivity.DurationSeconds,
                //    _currentWindowActivity.WindowId);
            }
            else
            {
                //Log.Debug("忽略持续时间 <1s 的窗口活动");
            }

            _currentWindowActivity = null;
        }

    }
}
