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
    /// 监听剪贴板内容变化，并将其记录到数据库。
    /// 继承 BackgroundService，将剪贴板监听器作为其生命周期的一部分。
    /// </summary>
    public class ClipboardCollectorService : BackgroundService
    {
        public event EventHandler ClipboardUpdated;
        private readonly IDbContextFactory<ActivityDbContext> _dbContextFactory;

        private readonly ClipboardMonitor _monitor;
        private string _lastContentHash;

        public ClipboardCollectorService(IDbContextFactory<ActivityDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;

            // 实例化和事件订阅保持不变
            _monitor = new ClipboardMonitor();
            _monitor.ClipboardUpdated += async (s, e) =>
            {
                Console.WriteLine("[ClipboardCollectorService] 剪贴板更新事件触发。");
                await HandleClipboardChangeAsync();
            };

            Console.WriteLine("[ClipboardCollectorService] 初始化完成，准备启动监听器...");
            // ❌ 移除 _monitor.Start();
        }

        // ClipboardCollectorService.cs
        // ✅ 保持服务活动直到收到停止信号
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _monitor.Start();
            Console.WriteLine("[ClipboardCollectorService] 剪贴板监听已启动。");

            // 通过循环维持服务运行状态
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken); // 适当延时减少CPU占用
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _monitor.Dispose();
            Console.WriteLine("[ClipboardCollectorService] 剪贴板监听已停止。");
            return base.StopAsync(cancellationToken);
        }

        private async Task HandleClipboardChangeAsync()
        {
            try
            {
                string content = PInvokeHelper.GetClipboardText();
                if (string.IsNullOrEmpty(content))
                {
                    Console.WriteLine("[ClipboardCollectorService] 剪贴板内容为空，忽略。");
                    return;
                }

                Console.WriteLine($"[ClipboardCollectorService] 获取剪贴板内容 (长度: {content.Length})");

                // 计算哈希
                string currentHash = HashHelper.CalculateSha256(content);
                Console.WriteLine($"[ClipboardCollectorService] 当前内容哈希: {currentHash}");

                // 防止重复记录
                if (currentHash == _lastContentHash)
                {
                    Console.WriteLine("[ClipboardCollectorService] 与上次内容相同，跳过保存。");
                    return;
                }

                var entry = new ClipboardEntry
                {
                    CopyTime = DateTime.Now,
                    Content = content,
                    ContentLength = content.Length,
                    ContentHash = currentHash
                };

                using var db = _dbContextFactory.CreateDbContext();
                db.ClipboardEntries.Add(entry);
                await db.SaveChangesAsync();

                _lastContentHash = currentHash;
                ClipboardUpdated?.Invoke(this, EventArgs.Empty);

                Console.WriteLine($"[ClipboardCollectorService] 已保存剪贴板内容 (时间: {entry.CopyTime}, 长度: {entry.ContentLength})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClipboardCollectorService] 保存错误: {ex}");
            }
        }
    }
}
