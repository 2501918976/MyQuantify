using SelfTracker.Entity;
using SelfTracker.Repository;
using System;
using System.Timers;

namespace SelfTracker.DataCollectors
{
    /// <summary>
    /// 数据采集总控制类：负责协调各个采集组件并将数据存入数据库。
    /// 使用 sealed 确保不可被继承。
    /// </summary>
    public sealed class DataCollector
    {
        // ================= 单例模式 =================
        // 使用 Lazy 确保线程安全的延迟初始化
        private static readonly Lazy<DataCollector> _instance = new Lazy<DataCollector>(() => new DataCollector());
        public static DataCollector Instance => _instance.Value;

        // ================= 采集组件 =================
        private readonly KeyboardCollector _keyboard;   // 键盘点击计数
        private readonly ClipboardCollector _clipboard; // 剪贴板动作监听
        private int _clipboardCountInInterval = 0;      // 内存中的增量计数（当前时间段内的复制次数）
        private DateTime _lastClipboardTime = DateTime.MinValue; // 防止重复触发的防抖变量

        private readonly ForegroundCollector _foreground; // 前台窗口追踪
        private readonly AFKCollector _afk;             // 挂机状态判定
        private readonly DataRepository _repo;          // 数据库操作仓库

        // ================= 控制变量 =================
        private readonly System.Timers.Timer _coreTimer; // 1秒一次的核心轮询定时器
        private long _currentSessionId;                  // 当前运行会话的 ID
        private int _logIntervalSeconds = 30;           // 默认每30秒将增量数据落库一次
        private int _secondsCounter = 0;                // 累加秒数计数器

        // ================= 状态追踪变量 (用于判断是否切换了窗口) =================
        private string _lastStateType = "None";          // Active 或 AFK
        private string _lastProcessName = "";            // 上一个进程名
        private string _lastWindowTitle = "";            // 上一个窗口标题
        private string _lastActivityType = "General";    // 追踪上一个活动所属的类别（如 Development）
        private DateTime _lastStateStartTime = DateTime.Now; // 当前状态开始的时间

        // ================= 增量计数变量 =================
        private int _lastKeyCountAtLastLog = 0;         // 上次记录时的键盘总数
        private DateTime _lastLogTime = DateTime.Now;   // 上次落库的时间点

        /// <summary>
        /// 私有构造函数：初始化所有组件并配置剪贴板防抖逻辑
        /// </summary>
        private DataCollector()
        {
            var dbService = new SQLiteDataService();
            _repo = new DataRepository(dbService.ConnectionString);

            _keyboard = new KeyboardCollector();
            _foreground = new ForegroundCollector();
            _clipboard = new ClipboardCollector();

            // 订阅剪贴板事件
            _clipboard.OnClipboardChanged += () =>
            {
                // 防抖逻辑：500毫秒内的多次触发（如某些软件的机制）视为同一次复制
                if ((DateTime.Now - _lastClipboardTime).TotalMilliseconds > 500)
                {
                    _clipboardCountInInterval++;
                    _lastClipboardTime = DateTime.Now;
                }
            };

            _afk = new AFKCollector();

            RefreshSettings(); // 加载配置信息

            // 初始化核心定时器，每 1000 毫秒（1秒）执行一次状态检查
            _coreTimer = new System.Timers.Timer(1000);
            _coreTimer.Elapsed += OnCoreTick;
        }

        /// <summary>
        /// 从设置文件中刷新落库频率等配置
        /// </summary>
        public void RefreshSettings()
        {
            var settings = SelfTracker.Setting.AppSettings.Load();
            _logIntervalSeconds = settings.LogIntervalSeconds;
            _secondsCounter = 0;
        }

        /// <summary>
        /// 启动采集逻辑：开启会话并启动所有监听钩子
        /// </summary>
        public void Start()
        {
            // 在数据库中创建新的会话记录并获取 ID
            _currentSessionId = _repo.StartSession();

            _keyboard.Start();   // 安装键盘钩子
            _clipboard.Start();  // 启动剪贴板窗口钩子
            _coreTimer.Start();  // 开启秒表轮询

            _lastStateStartTime = DateTime.Now;
            _lastLogTime = DateTime.Now;
        }

        /// <summary>
        /// 每秒执行一次的逻辑：检测用户是否走开，或者是否切换了软件
        /// </summary>
        private void OnCoreTick(object sender, ElapsedEventArgs e)
        {
            var idleTime = _afk.IdleTime;
            var fgInfo = _foreground.GetCurrentInfo();

            // --- 1. 状态基本判定 ---
            string currentType = idleTime.TotalMinutes >= 1 ? "AFK" : "Active";
            string currentProcess = (currentType == "AFK") ? "Idle" : (fgInfo?.ProcessName ?? "Unknown");
            string currentTitle = (currentType == "AFK") ? "User AFK" : (fgInfo?.WindowTitle ?? "");

            // --- 2. 核心修改：动态确认 ActivityType ---
            string currentActivityType;
            if (currentType == "AFK")
            {
                currentActivityType = "AFK";
            }
            else
            {
                // 尝试从缓存映射中获取用户定义的类型（例如 "devenv" -> "Coding"）
                // 如果用户还没在 UI 页面分类，则暂时标记为 "General"
                if (!_categoryCache.TryGetValue(currentProcess, out var mappedType))
                {
                    currentActivityType = "General";
                }
                else
                {
                    currentActivityType = mappedType;
                }
            }

            // --- 3. 检测状态切换（软件切换或 AFK 切换） ---
            // 逻辑：如果状态类型变了（Active<->AFK）或者进程变了，就认为是一次“账目结算”
            if (currentType != _lastStateType || (currentType == "Active" && currentProcess != _lastProcessName))
            {
                if (_lastStateType != "None")
                {
                    // 结算旧状态的时长
                    LogStateTransition(_lastStateType, _lastProcessName, _lastWindowTitle, _lastActivityType, _lastStateStartTime, DateTime.Now);

                    // 结算旧状态期间的生产力数据
                    LogProductivity();
                    _secondsCounter = 0;
                }

                // 更新状态追踪变量，为下一段计时做准备
                _lastStateType = currentType;
                _lastProcessName = currentProcess;
                _lastWindowTitle = currentTitle;
                _lastActivityType = currentActivityType; // 存储本次匹配到的类型
                _lastStateStartTime = DateTime.Now;
            }

            // --- 4. 增量定时落库逻辑 ---
            // 即使窗口没切换，每隔 30 秒也要存一次按键数，防止程序崩溃丢数据
            _secondsCounter++;
            if (_secondsCounter >= _logIntervalSeconds)
            {
                LogProductivity();
                _secondsCounter = 0;
            }
        }

        /// <summary>
        /// 将状态切换记录（活动日志或挂机日志）存入对应的数据库表
        /// </summary>
        private void LogStateTransition(string type, string process, string title, string actType, DateTime start, DateTime end)
        {
            // 忽略小于 1 秒的瞬间切换，减少数据库噪音
            if ((end - start).TotalSeconds < 1) return;

            if (type == "AFK")
            {
                _repo.InsertAfk(new AfkLog
                {
                    StartTime = start,
                    EndTime = end,
                    SessionId = (int)_currentSessionId
                });
            }
            else
            {
                _repo.InsertActivity(new ActivityLog
                {
                    ProcessName = process,
                    WindowTitle = title,
                    StartTime = start,
                    EndTime = end,
                    Duration = (int)(end - start).TotalSeconds,
                    SessionId = (int)_currentSessionId,
                    ActivityType = actType
                });
            }
        }

        /// <summary>
        /// 记录生产力数据（按键数和复制数）
        /// </summary>
        private void LogProductivity()
        {
            int currentKeys = _keyboard.KeyCount;
            int keyDiff = currentKeys - _lastKeyCountAtLastLog; // 计算自上次记录以来的增量
            int copyDiff = _clipboardCountInInterval;        // 获取当前的复制次数增量
            DateTime now = DateTime.Now;

            // 只有产生有效数据时才写入数据库，节省空间
            if (keyDiff > 0 || copyDiff > 0)
            {
                _repo.InsertProductivity(new ProductivityCount
                {
                    Keystrokes = keyDiff,
                    CopyCount = copyDiff,
                    SessionId = (int)_currentSessionId,
                    PeriodStart = _lastLogTime,
                    PeriodSeconds = (int)(now - _lastLogTime).TotalSeconds
                });

                // 更新偏移量，并将内存计数清零
                _lastKeyCountAtLastLog = currentKeys;
                _clipboardCountInInterval = 0;
            }
            _lastLogTime = now;
        }

        /// <summary>
        /// 【公开方法】强制立即将内存中所有未保存的数据写入数据库。
        /// 用于程序关闭或用户点击“同步”按钮时。
        /// </summary>
        public void ForceLogToDb()
        {
            lock (this) // 线程锁，防止与定时器 OnCoreTick 产生冲突
            {
                LogProductivity();

                DateTime now = DateTime.Now;
                if (_lastStateType != "None")
                {
                    LogStateTransition(_lastStateType, _lastProcessName, _lastWindowTitle, _lastActivityType, _lastStateStartTime, now);
                }

                _lastStateStartTime = now;
                _secondsCounter = 0; // 重置定时器计数
            }
        }

        /// <summary>
        /// 停止采集：结清所有账目并注销系统钩子
        /// </summary>
        public void Stop()
        {
            _coreTimer.Stop();

            // 记录最后一段状态
            LogStateTransition(_lastStateType, _lastProcessName, _lastWindowTitle, _lastActivityType, _lastStateStartTime, DateTime.Now);
            LogProductivity();

            // 在数据库中标记 Session 结束
            _repo.UpdateSessionEnd(_currentSessionId);

            _keyboard?.Dispose();
            // 剪贴板监听器无需 dispose，因为它依赖于 HwndSource 的生命周期
        }


        #region 分类

        // 在 DataCollector 类中添加规则缓存
        private Dictionary<string, string> _categoryCache = new Dictionary<string, string>();

        // 在构造函数或 Start 方法中加载规则
        public void LoadRules()
        {
            _categoryCache = _repo.GetAllCategoryRules();
        }

        #endregion
    }
}

