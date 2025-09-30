using Microsoft.EntityFrameworkCore;
using MyQuantifyApp.DataCollector.Models;
using System.Linq;

namespace MyQuantifyApp.DataCollector.Storage
{
    /// <summary>
    /// EF Core 数据库上下文，用于管理所有日志模型的持久化。
    /// 负责连接数据库，映射 C# 模型到数据库表，并配置关系。
    /// </summary>
    public class ActivityDbContext : DbContext
    {
        // =================================================================
        // 数据库集 (DbSet): 映射到数据库中的表
        // =================================================================

        /// <summary>
        /// 应用程序/窗口活动会话记录表。
        /// </summary>
        public DbSet<ActivitySession> ActivitySessions { get; set; }

        /// <summary>
        /// 剪贴板复制操作记录表。
        /// </summary>
        public DbSet<ClipboardEntry> ClipboardEntries { get; set; }

        /// <summary>
        /// 用户离开键盘 (AFK) 状态记录表。
        /// </summary>
        public DbSet<AfkLog> AfkLogs { get; set; }

        /// <summary>
        /// 聚合的打字次数记录表。
        /// </summary>
        public DbSet<TypingCount> TypingCounts { get; set; }

        // =================================================================
        // 构造函数
        // =================================================================

        /// <summary>
        /// 接受外部配置选项的构造函数 (用于依赖注入或测试)。
        /// </summary>
        /// <param name="options">DbContext 配置选项。</param>
        public ActivityDbContext(DbContextOptions<ActivityDbContext> options) : base(options)
        {
            // 继承自上一个版本，保持兼容性
        }

        /// <summary>
        /// 默认构造函数。当未通过 DI 传递配置时调用。
        /// </summary>
        public ActivityDbContext() : base()
        {
            // 在此版本中，如果未启用迁移，并且模型已更改，可能需要手动调用 Database.EnsureCreated() 或 Database.Migrate()
        }

        // =================================================================
        // 配置 (连接字符串)
        // =================================================================

        /// <summary>
        /// 配置数据库连接选项。
        /// </summary>
        /// <param name="optionsBuilder">用于配置上下文的构建器。</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 只有当 optionsBuilder 尚未配置 (例如在默认构造函数中调用) 时，才应用默认连接字符串。
            if (!optionsBuilder.IsConfigured)
            {
                // 启用共享缓存 (Cache=Shared)，避免多个 DbContext 冲突
                // WAL 模式建议通过 PRAGMA 设置（见下一步）
                optionsBuilder.UseSqlite("Data Source=ActivityLog.db;Cache=Shared;Mode=ReadWriteCreate");
            }
        }

        // =================================================================
        // 模型创建和关系配置
        // =================================================================

        /// <summary>
        /// 配置模型创建期间的细节，包括数据类型和关系。
        /// </summary>
        /// <param name="modelBuilder">用于构建模型的构建器。</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. ClipboardEntry 配置：确保 Content 字段使用 TEXT 数据类型，以支持较长的文本。
            modelBuilder.Entity<ClipboardEntry>()
                .Property(e => e.Content)
                .HasColumnType("TEXT");

            // 2. ActivitySession 自引用关系配置： PreviousSession
            // 允许通过 PreviousSessionId 链接到前一个活动会话，用于追踪用户行为流。
            modelBuilder.Entity<ActivitySession>()
                .HasOne(s => s.PreviousSession)         // 当前会话有一个 PreviousSession
                .WithMany()                             // PreviousSession 可以有多个后续会话
                .HasForeignKey(s => s.PreviousSessionId) // 外键是 PreviousSessionId
                .IsRequired(false)                      // 允许 PreviousSessionId 为空 (例如第一个会话)
                .OnDelete(DeleteBehavior.Restrict);      // 防止删除会话时级联删除链条中的其他会话

            // 3. TypingCount 与 ActivitySession 的关系配置
            modelBuilder.Entity<TypingCount>()
                .HasOne(t => t.ActivitySession)
                .WithMany()
                .HasForeignKey(t => t.ActivitySessionId)
                .IsRequired(false)                  // 外键可空
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
