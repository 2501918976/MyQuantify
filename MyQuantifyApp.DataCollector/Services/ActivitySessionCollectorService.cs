using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting; // 引入 BackgroundService 基类
using MyQuantifyApp.DataCollector.Models;
using MyQuantifyApp.DataCollector.Storage;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MyQuantifyApp.DataCollector.Utilities.PInvokeHelper;

namespace MyQuantifyApp.DataCollector.Services
{
    /// <summary>
    /// 监测当前活跃窗口，记录活动会话。
    /// 继承自 BackgroundService，以利用 Host 提供的生命周期管理。
    /// </summary>
    public class ActivitySessionCollectorService : BackgroundService
    {
        // 核心优化：使用工厂模式创建 DbContext，确保每个保存操作都是线程安全的。
        private readonly IDbContextFactory<ActivityDbContext> _dbContextFactory;
        private readonly int _pollIntervalMs;
        private ActivitySession _currentSession;

        // 构造函数现在接受 DbContext 工厂
        public ActivitySessionCollectorService(IDbContextFactory<ActivityDbContext> dbContextFactory, int pollIntervalMs = 1000)
        {
            _dbContextFactory = dbContextFactory;
            _pollIntervalMs = pollIntervalMs;
        }

        /// <summary>
        /// 后台服务的核心执行循环。
        /// </summary>
        /// <param name="stoppingToken">宿主提供的取消令牌，用于优雅地停止服务。</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Console.WriteLine("[ActivitySessionCollectorService] 启动活动会话监测...");

            // 循环直到服务收到停止信号
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    IntPtr hWnd = GetForegroundWindow();
                    if (hWnd == IntPtr.Zero)
                    {
                        // 窗口为空（可能在锁屏或用户切换中），跳过当前循环
                        await Task.Delay(_pollIntervalMs, stoppingToken);
                        continue;
                    }

                    // 1. 获取窗口标题
                    StringBuilder sb = new StringBuilder(512);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();

                    // 2. 获取进程名
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    string processName = GetProcessNameByPid(pid);

                    // 3. 判断是否切换了窗口
                    if (_currentSession == null || _currentSession.WindowTitle != title || _currentSession.ProcessName != processName)
                    {
                        await EndCurrentSessionAsync(); // 异步结束旧会话
                        StartNewSession(title, processName); // 启动新会话
                    }

                    // 4. 更新性能指标（可在这里执行）
                    UpdatePerformanceMetrics(hWnd, pid);

                }
                catch (TaskCanceledException)
                {
                    // 接收到停止信号，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"[ActivitySessionCollectorService] 循环错误: {ex.Message}");
                }

                await Task.Delay(_pollIntervalMs, stoppingToken);
            }

            // 服务停止前，确保结束并保存最后的会话
            await EndCurrentSessionAsync();
            //Console.WriteLine("[ActivitySessionCollectorService] 监测已停止。");
        }

        /// <summary>
        /// 通过 PID 获取进程名称（封装旧逻辑）。
        /// </summary>
        private string GetProcessNameByPid(uint pid)
        {
            try
            {
                Process proc = Process.GetProcessById((int)pid);
                //Console.WriteLine($"[ActivitySessionCollectorService] 获取进程 PID={pid}, 名称={proc.ProcessName}");
                return proc.ProcessName;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[ActivitySessionCollectorService] 获取进程失败 PID={pid}, 错误={ex.Message}");
                return "Unknown";
            }
        }

        private void StartNewSession(string title, string processName)
        {
            //Console.WriteLine($"[ActivitySessionCollectorService] 新窗口切换: 标题=\"{title}\", 进程={processName}");

            var session = new ActivitySession
            {
                WindowTitle = title,
                ProcessName = processName,
                StartTime = DateTime.Now,
            };

            if (_currentSession != null && _currentSession.Id != 0)
            {
                session.PreviousSessionId = _currentSession.Id;
                //Console.WriteLine($"[ActivitySessionCollectorService] 设置 PreviousSessionId={_currentSession.Id}");
            }

            _currentSession = session;
            //Console.WriteLine($"[ActivitySessionCollectorService] 新会话已创建: StartTime={session.StartTime}");
        }

        private async Task EndCurrentSessionAsync()
        {
            if (_currentSession != null)
            {
                _currentSession.EndTime = DateTime.Now;
                _currentSession.DurationSeconds = (int)(_currentSession.Duration.TotalSeconds);

                //Console.WriteLine($"[ActivitySessionCollectorService] 正在结束会话: 窗口={_currentSession.WindowTitle}, 进程={_currentSession.ProcessName}, 持续={_currentSession.DurationSeconds}s");

                try
                {
                    using var db = _dbContextFactory.CreateDbContext();

                    if (_currentSession.Id == 0)
                    {
                        db.ActivitySessions.Add(_currentSession);
                        //Console.WriteLine($"[ActivitySessionCollectorService] 新会话写入数据库...");
                    }
                    else
                    {
                        db.ActivitySessions.Update(_currentSession);
                        //Console.WriteLine($"[ActivitySessionCollectorService] 更新已有会话 ID={_currentSession.Id}...");
                    }

                    await db.SaveChangesAsync();
                    //Console.WriteLine($"[ActivitySessionCollectorService] 会话保存成功.");
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"[ActivitySessionCollectorService] 保存失败: {ex.Message}");
                }

                _currentSession = null;
            }
        }

        private void UpdatePerformanceMetrics(IntPtr hWnd, uint pid)
        {
            if (_currentSession == null) return;
            try
            {
                var proc = Process.GetProcessById((int)pid);
                _currentSession.IsFullscreen = IsZoomed(hWnd);
                _currentSession.AvgMemoryUsageMB = proc.WorkingSet64 / (1024 * 1024);

                //Console.WriteLine($"[ActivitySessionCollectorService] 性能指标: Fullscreen={_currentSession.IsFullscreen}, 内存={_currentSession.AvgMemoryUsageMB}MB");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[ActivitySessionCollectorService] 更新性能指标失败: {ex.Message}");
            }
        }

    }
}
