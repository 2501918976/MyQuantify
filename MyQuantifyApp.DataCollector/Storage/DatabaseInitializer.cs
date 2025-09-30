using Microsoft.EntityFrameworkCore;
using MyQuantifyApp.DataCollector.Storage;
using System;
using System.Linq; // 确保包含 System.Linq

namespace MyQuantifyApp.DataCollector.Storage
{
    /// <summary>
    /// 负责在应用程序启动时初始化数据库结构。
    /// 使用 Database.EnsureCreated() 来创建数据库和表，以跳过 EF Core 迁移。
    /// </summary>
    public class DatabaseInitializer
    {
        /// <summary>
        /// 执行数据库初始化。
        /// </summary>
        //public static void Initialize()
        //{
        //    try
        //    {
        //        // 1. 实例化 DbContext
        //        // DbContext 会使用 OnConfiguring 中定义的 SQLite 连接字符串 (ActivityLog.db)
        //        using (var db = new ActivityDbContext())
        //        {
        //            // 2. 检查数据库和所有表是否已存在。
        //            //    如果不存在，则根据当前模型定义创建数据库文件和所有必需的表。
        //            bool created = db.Database.EnsureCreated();

        //            if (created)
        //            {
        //                // 如果数据库文件是首次创建
        //                Console.WriteLine("SQLite 数据库文件和所有表已成功创建。");
        //            }
        //            else
        //            {
        //                // 如果数据库文件已存在，则什么也不做。
        //                Console.WriteLine("SQLite 数据库已存在，跳过创建。");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // 捕获并记录任何数据库连接或创建错误
        //        Console.WriteLine($"数据库初始化失败: {ex.Message}");
        //        // 在 Windows Service 中，这应该记录到系统日志或文件日志中。
        //    }
        //}

        public static void Initialize()
        {
            try
            {
                using (var db = new ActivityDbContext())
                {
                    bool created = db.Database.EnsureCreated();

                    // ✅ 启用 WAL 模式（多读一写，解决 database is locked）
                    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

                    if (created)
                        Console.WriteLine("SQLite 数据库文件和所有表已成功创建 (已启用 WAL 模式)。");
                    else
                        Console.WriteLine("SQLite 数据库已存在，跳过创建 (已启用 WAL 模式)。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据库初始化失败: {ex.Message}");
            }
        }

    }
}
