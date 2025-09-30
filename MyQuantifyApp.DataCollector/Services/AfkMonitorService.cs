using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting; // 引入 BackgroundService 基类
using MyQuantifyApp.DataCollector.Models;
using MyQuantifyApp.DataCollector.Storage;
using MyQuantifyApp.DataCollector.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyQuantifyApp.DataCollector.Services
{
    /// <summary>
    /// 持续监测用户空闲时间，记录 AFK (离开键盘) 日志。
    /// 继承自 BackgroundService，使用 ExecuteAsync 循环取代 System.Threading.Timer。
    /// </summary>
    public class AfkMonitorService : BackgroundService
    {
        private readonly IDbContextFactory<ActivityDbContext> _dbContextFactory;
        private readonly int _pollIntervalMs;
        private readonly int _afkThresholdSeconds;
        private AfkLog _currentAfk;

        public AfkMonitorService(IDbContextFactory<ActivityDbContext> dbContextFactory, int afkThresholdSeconds = 20, int pollIntervalMs = 1000)
        {
            _dbContextFactory = dbContextFactory;
            _afkThresholdSeconds = afkThresholdSeconds;
            _pollIntervalMs = pollIntervalMs;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Console.WriteLine("[AfkMonitorService] 启动 AFK 监测...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 获取自上次输入以来的空闲时间（秒）
                    uint idleSeconds = PInvokeHelper.GetIdleTimeInSeconds();

                    //Console.WriteLine($"[AfkMonitorService] 当前空闲时间: {idleSeconds}s (阈值: {_afkThresholdSeconds}s)");

                    if (idleSeconds >= _afkThresholdSeconds)
                    {
                        // 用户处于 AFK 状态
                        if (_currentAfk == null)
                        {
                            // 首次进入 AFK 状态，记录开始时间
                            _currentAfk = new AfkLog
                            {
                                AfkStartTime = DateTime.Now.AddSeconds(-idleSeconds),
                                LastActivityTime = DateTime.Now.AddSeconds(-idleSeconds)
                            };

                            //Console.WriteLine($"[AfkMonitorService] >>> 用户进入 AFK 状态, 开始时间: {_currentAfk.AfkStartTime}");
                        }
                        else
                        {
                            // 持续 AFK 状态，更新最后活动时间
                            var last = _currentAfk.LastActivityTime;
                            _currentAfk.LastActivityTime = DateTime.Now.AddSeconds(-idleSeconds);

                            //Console.WriteLine($"[AfkMonitorService] 用户仍在 AFK, 更新 LastActivityTime: {last} -> {_currentAfk.LastActivityTime}");
                        }
                    }
                    else
                    {
                        // 用户恢复活动状态
                        if (_currentAfk != null)
                        {
                            _currentAfk.ReturnTime = DateTime.Now;
                            _currentAfk.AfkDurationSeconds = (int)(_currentAfk.ReturnTime.Value - _currentAfk.AfkStartTime).TotalSeconds;

                            using (var db = _dbContextFactory.CreateDbContext())
                            {
                                db.AfkLogs.Add(_currentAfk);
                                await db.SaveChangesAsync(stoppingToken);
                            }

                            //Console.WriteLine($"[AfkMonitorService] <<< 用户恢复活动, 结束时间: {_currentAfk.ReturnTime}, AFK 时长: {_currentAfk.AfkDurationSeconds}s");
                            _currentAfk = null; // 清空当前 AFK 记录
                        }
                        else
                        {
                            //Console.WriteLine("[AfkMonitorService] 用户活跃, 未处于 AFK 状态");
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    //Console.WriteLine("[AfkMonitorService] 任务被取消，准备退出循环...");
                    break;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"[AfkMonitorService] 循环错误: {ex}");
                }

                // 按轮询间隔等待
                await Task.Delay(_pollIntervalMs, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            //Console.WriteLine("[AfkMonitorService] StopAsync 被调用，检查是否有未保存的 AFK 会话...");

            if (_currentAfk != null)
            {
                _currentAfk.ReturnTime = DateTime.Now;
                _currentAfk.AfkDurationSeconds = (int)(_currentAfk.ReturnTime.Value - _currentAfk.AfkStartTime).TotalSeconds;

                using (var db = _dbContextFactory.CreateDbContext())
                {
                    db.AfkLogs.Add(_currentAfk);
                    await db.SaveChangesAsync(cancellationToken);
                }

                //Console.WriteLine($"[AfkMonitorService] 已保存未完成的 AFK 会话, 时长: {_currentAfk.AfkDurationSeconds}s");
            }

            await base.StopAsync(cancellationToken);
            //Console.WriteLine("[AfkMonitorService] 已停止。");
        }
    }
}
