using SelfTracker.Controllers;
using SelfTracker.Entity;
using SelfTracker.Entity.Base;
using SelfTracker.Repository;
using SelfTracker.Repository.Base;
using SelfTracker.Setting;
using System;
using System.Timers;

namespace SelfTracker.DataCollectors
{
    /// <summary>
    /// 数据采集核心调度器（单例）
    /// 负责协调各个控制器、管理数据库生命周期、并根据配置执行定时任务
    /// </summary>
    public sealed class DataCollector : IDisposable
    {
        private static readonly Lazy<DataCollector> _instance = new Lazy<DataCollector>(() => new DataCollector());
        public static DataCollector Instance => _instance.Value;

        // --- 核心组件 ---
        private readonly QuantifyDbContext _db;
        private readonly AppSettings _settings;
        private readonly System.Timers.Timer _coreTimer;

        // --- 控制器引用 ---
        private readonly SystemStateController _systemStateCtrl;
        private readonly ForegroundController _foregroundCtrl;
        private readonly KeyboardController _keyboardCtrl;
        private readonly CopyController _copyCtrl;

        // --- 状态追踪 ---
        private DateTime _lastFlushTime = DateTime.Now;
        private bool _isDisposed = false;

        private DataCollector()
        {
            // 1. 初始化物理数据库（确保文件和表结构存在）
            // 这样可以防止 EF Core 在表还没创建时就尝试访问
            var dbInitializer = new SQLiteDataService();

            // 2. 加载配置（实际建议通过 SettingsManager 从本地 Json 加载）
            _settings = new AppSettings();

            // 3. 初始化数据库上下文
            _db = new QuantifyDbContext();

            // 4. 初始化系统状态控制器 (处理开机/Session/AFK)
            // 将设置中的 AFK 超时时间传入
            _systemStateCtrl = new SystemStateController(_db, TimeSpan.FromSeconds(_settings.AFKTimeoutSeconds));

            // 关键：强制执行一次 SaveChanges，确保初始 Session 获得数据库 ID
            // 防止后续 TypingLog 等由于外键 ID 为 0 而报错
            _db.SaveChanges();

            // 5. 获取当前已持久化的 Session 对象
            var initialSession = _systemStateCtrl.GetCurrentSession();

            // 6. 初始化 Repository 体系
            var activityRepo = new ActivityLogRepository(_db);
            var processRepo = new ProcessInfoRepository(_db);
            var typingRepo = new TypingLogRepository(_db);
            var copyRepo = new CopyLogRepository(_db);
            var matcher = new CategoryMatcher(_db);

            // 7. 初始化功能控制器
            _foregroundCtrl = new ForegroundController(activityRepo, processRepo, matcher);
            _keyboardCtrl = new KeyboardController(typingRepo, initialSession);
            _copyCtrl = new CopyController(copyRepo, initialSession);

            // 8. 配置核心定时器（每秒/每几秒巡检一次）
            _coreTimer = new System.Timers.Timer(_settings.CoreTickIntervalMs);
            _coreTimer.Elapsed += OnCoreTick;
        }

        /// <summary>
        /// 启动采集任务
        /// </summary>
        public void Start()
        {
            _keyboardCtrl.Start();
            _copyCtrl.Start();
            _coreTimer.Start();
        }

        /// <summary>
        /// 核心巡检周期：处理状态切换、窗口采集、定时入库
        /// </summary>
        private void OnCoreTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                // A. 检查并更新系统状态 (AFK检测/电源状态)
                _systemStateCtrl.CheckUserState();

                // B. 获取当前最新的 Session (可能在 CheckUserState 中已切换)
                var currentSession = _systemStateCtrl.GetCurrentSession();

                // C. 同步 Session 给子控制器
                _keyboardCtrl.SetCurrentSession(currentSession);
                _copyCtrl.SetCurrentSession(currentSession);

                // D. 窗口采集：仅在用户“活跃使用”时频繁采集
                if (currentSession.Type == SystemStateType.ActiveUsing)
                {
                    // 注意：CaptureCurrentActivity 内部应包含 SaveChanges 或在这里统一保存
                    _foregroundCtrl.CaptureCurrentActivity(currentSession);
                }

                // E. 生产力数据定时冲刷（根据 AppSettings 配置的秒数）
                if ((DateTime.Now - _lastFlushTime).TotalSeconds >= _settings.LogIntervalSeconds)
                {
                    FlushProductivityData();
                }
            }
            catch (Exception ex)
            {
                // 建议增加日志记录，防止定时器线程异常崩溃
                System.Diagnostics.Debug.WriteLine($"DataCollector Tick Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量将内存中的打字、复制等统计数据写入数据库
        /// </summary>
        public void FlushProductivityData()
        {
            lock (_db) // 简单的线程锁，防止 Hook 线程与定时器线程竞争 DbContext
            {
                // 调用各个控制器的 Flush 方法，将累加的 keyCount 等存入 Repository
                _keyboardCtrl.Flush();
                _copyCtrl.Flush();

                // 统一持久化到磁盘
                _db.SaveChanges();
            }
            _lastFlushTime = DateTime.Now;
        }

        /// <summary>
        /// 停止采集并安全释放
        /// </summary>
        public void Stop()
        {
            // 1. 停止定时器，防止在关闭过程中再次触发 OnCoreTick
            _coreTimer?.Stop();

            // 2. 【关键】瞬间冲刷最后一次内存数据
            // 这会将 KeyboardController 和 CopyController 内存里的计数器转换成实体并 Add 到 DbContext
            FlushProductivityData();

            // 3. 停止各个子控制器（注销钩子等）
            _keyboardCtrl?.Stop();
            _copyCtrl?.Stop();
            _systemStateCtrl?.Dispose();

            // 4. 最后的持久化：确保所有 Add 进去的记录真正写入磁盘文件
            try
            {
                lock (_db)
                {
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭时保存失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _db?.Dispose();
                _isDisposed = true;
            }
        }
    }
}