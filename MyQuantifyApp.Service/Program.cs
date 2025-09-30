using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using MyQuantifyApp.DataCollector.Services;
using MyQuantifyApp.DataCollector.Storage;
using System;

var host = Host.CreateDefaultBuilder(args)
    // 步骤 1: 强制设置全局最低日志级别为 Warning，以抑制大部分默认的 Info 消息
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Warning);
    })
    .UseWindowsService()
    .ConfigureServices((context, services) =>
    {
        services.AddDbContextFactory<ActivityDbContext>(options =>
        {
            options.UseSqlite("Data Source=ActivityLog.db");

            // 步骤 2: 关键修复，使用 RelationalEventId.CommandExecuted 忽略 SQL 执行日志
            // 这是解决 CS0117 错误的正确常量
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.CommandExecuted));

            // 保持 LogTo 配置，确保其他 EF Core 警告/错误日志能够被捕获
            options.LogTo(Console.WriteLine, LogLevel.Warning);
        });

        services.AddHostedService<ActivitySessionCollectorService>();
        services.AddHostedService<TypingCountService>();
        services.AddHostedService<ClipboardCollectorService>();
        services.AddHostedService<AfkMonitorService>();
    })
    .Build();

try
{
    Console.WriteLine("[Database] 开始初始化或检查数据库...");
    DatabaseInitializer.Initialize();
    Console.WriteLine("[Database] 数据库检查完成。");
}
catch (Exception ex)
{
    Console.WriteLine($"[Database] 致命错误: 数据库初始化失败: {ex.Message}");
    return;
}

await host.RunAsync();
