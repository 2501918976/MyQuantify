using MyQuantifyApp.Database.Models;
using MyQuantifyApp.Database.Repositories.Raw;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace MyQuantifyApp.Service
{

    public class DataFlushService
    {
        // ====================================================================
        // 1. 依赖项 (Dependencies)
        // ====================================================================

        private readonly ActivityMonitorService _monitorService;

        // 依赖所有数据仓储
        private readonly KeyCharDataRepository _keyRepository;
        private readonly WindowActivityRepository _windowActivityRepository;
        private readonly WindowRepository _windowRepository;
        private readonly ClipboardActivityDataRepository _clipboardRepository;
        private readonly AfkActivityDataRepository _afkActivityDataRepository;

        // ====================================================================
        // 2. 配置和定时器
        // ====================================================================

        private const double BATCH_INTERVAL_MS = 5000;
        private System.Timers.Timer _batchInsertTimer;

        // ====================================================================
        // 3. 构造函数 (Constructor)
        // ====================================================================

        public DataFlushService(
            ActivityMonitorService monitorService,
            KeyCharDataRepository keyRepository,
            WindowRepository windowRepository,
            WindowActivityRepository windowActivityRepository,
            ClipboardActivityDataRepository clipboardRepository,
            AfkActivityDataRepository afkRepository)
        {
            _monitorService = monitorService;
            _keyRepository = keyRepository;
            _windowRepository = windowRepository;
            _windowActivityRepository = windowActivityRepository;
            _clipboardRepository = clipboardRepository;
            _afkActivityDataRepository = afkRepository;

            _batchInsertTimer = new System.Timers.Timer(BATCH_INTERVAL_MS);
            _batchInsertTimer.Elapsed += OnBatchInsertTimerElapsed;
            _batchInsertTimer.AutoReset = true;
        }

        // ====================================================================
        // 4. 生命周期的控制方法
        // ====================================================================

        public void StartFlushing()
        {
            _batchInsertTimer.Start();
            //Log.Information("DataFlushService：批量插入定时器已启动，间隔 {Interval} 毫秒.", BATCH_INTERVAL_MS);
        }

        public void StopFlushing()
        {
            _batchInsertTimer?.Stop();
            //Log.Information("DataFlushService：正在执行最后一次批量写入...");
            FlushAllBuffers();
        }

        // ====================================================================
        // 5. 定时器事件和主要刷新逻辑
        // ====================================================================

        private void OnBatchInsertTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _monitorService.CheckAfkStatus();
            FlushAllBuffers();
        }

        public void FlushAllBuffers()
        {
            // 异步执行批量写入
            Task.Run(() =>
            {
                FlushKeyBuffer();
                FlushWindowBuffer();
                FlushClipboardBuffer();
                FlushAfkBuffer();
            });
        }

        // ====================================================================
        // 6. 批量写入实现 (Flush Implementations)
        // ❗ 注意：以下三个方法已修改为使用循环调用单条插入方法。
        // ====================================================================

        /// <summary>
        /// 批量写入按键数据。仍假定 KeyCharDataRepository 有 AddKeyLogs 批量方法。
        /// </summary>
        public void FlushKeyBuffer()
        {
            lock (_monitorService._keyCharBuffer)
            {
                if (_monitorService._keyCharBuffer.Count == 0) return;

                var dataToFlush = _monitorService._keyCharBuffer.ToList();
                _monitorService._keyCharBuffer.Clear();

                try
                {
                    // 保持原样，假定 _keyRepository 支持批量插入
                    _keyRepository.AddKeyLogs(dataToFlush);
                    //Log.Debug("DataFlushService：成功批量插入 {Count} 条按键数据.", dataToFlush.Count);
                }
                catch (Exception ex)
                {
                    //Log.Error(ex, "DataFlushService：批量插入按键数据时发生错误.");
                }
            }
        }

        private void FlushWindowBuffer()
        {
            lock (_monitorService._windowBuffer)
            {
                if (_monitorService._windowBuffer.Count == 0) return;

                // 步骤 1: 复制和清空缓冲区
                var activitiesToFlush = _monitorService._windowBuffer.ToList();
                _monitorService._windowBuffer.Clear();

                try
                {
                    // --------------------------------------------------------------------------
                    // 修正点: 添加 LINQ Select 语句进行类型映射/转换
                    // --------------------------------------------------------------------------
                    var modelsToInsert = activitiesToFlush.Select(data => new WindowActivity // 注意这里使用 WindowActivity 模型类型
                    {
                        WindowId = data.WindowId,
                        StartTime = data.StartTime,
                        EndTime = data.EndTime,
                        DurationSeconds = data.DurationSeconds
                    });

                    // 步骤 2: 调用仓储的批量插入方法
                    // 现在 modelsToInsert 是正确的 IEnumerable<WindowActivity> 类型
                    _windowActivityRepository.AddBatchWindowActivities(modelsToInsert);

                    // Log.Debug("DataFlushService：成功批量插入 {Count} 条窗口活动数据.", activitiesToFlush.Count);
                }
                catch (Exception ex)
                {
                    // Log.Error(ex, "DataFlushService：批量插入窗口活动数据时发生错误.");
                }
            }
        }

        private void FlushClipboardBuffer()
        {
            lock (_monitorService._clipboardBuffer)
            {
                if (_monitorService._clipboardBuffer.Count == 0) return;

                var dataToFlush = _monitorService._clipboardBuffer.ToList();
                _monitorService._clipboardBuffer.Clear();

                try
                {
                    // ❗ 修改点：使用循环调用 ClipboardActivityDataRepository.AddClipboardLog(log)
                    foreach (var log in dataToFlush)
                    {
                        _clipboardRepository.AddClipboardLog(log);
                    }
                    //Log.Debug("DataFlushService：成功批量插入 {Count} 条剪贴板数据.", dataToFlush.Count);
                }
                catch (Exception ex)
                {
                    //Log.Error(ex, "DataFlushService：批量插入剪贴板数据时发生错误.");
                }
            }
        }

        private void FlushAfkBuffer()
        {
            lock (_monitorService._afkBuffer)
            {
                if (_monitorService._afkBuffer.Count == 0) return;

                var dataToFlush = _monitorService._afkBuffer.ToList();
                _monitorService._afkBuffer.Clear();

                try
                {
                    // ❗ 修改点：使用循环调用 AfkActivityDataRepository.AddAfkLog(log)
                    foreach (var log in dataToFlush)
                    {
                        _afkActivityDataRepository.AddAfkLog(log);
                    }
                    //Log.Debug("DataFlushService：成功批量插入 {Count} 条 AFK 活动数据.", dataToFlush.Count);
                }
                catch (Exception ex)
                {
                    //Log.Error(ex, "DataFlushService：批量插入 AFK 活动数据时发生错误.");
                }
            }
        }
    }
}