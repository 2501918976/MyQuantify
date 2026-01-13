using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace SelfTracker.Repository
{
    public class QuantifyDbContext : DbContext
    {
        public DbSet<ProcessInfo> Processes { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<CategoryRule> CategoryRules { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public DbSet<TypingLog> TypingLogs { get; set; } = null!;
        public DbSet<CopyLog> CopyLogs { get; set; } = null!;
        public DbSet<SystemStateLog> SystemStateLogs { get; set; } = null!;
        public DbSet<Score> Scores { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "quantify.sqlite");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. 显式映射表名（必须与 SQLiteDataService 中的 SQL 脚本完全一致）
            modelBuilder.Entity<ProcessInfo>().ToTable("ProcessInfos");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<CategoryRule>().ToTable("CategoryRules");
            modelBuilder.Entity<ActivityLog>().ToTable("ActivityLogs");
            modelBuilder.Entity<TypingLog>().ToTable("TypingLogs");
            modelBuilder.Entity<CopyLog>().ToTable("CopyLogs");
            modelBuilder.Entity<SystemStateLog>().ToTable("SystemStateLogs");
            modelBuilder.Entity<Score>().ToTable("Scores");

            // 2. 索引配置（保持你原有的逻辑）
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<ActivityLog>().HasIndex(a => a.StartTime);
            modelBuilder.Entity<TypingLog>().HasIndex(t => t.StartTime);
            modelBuilder.Entity<CopyLog>().HasIndex(c => c.StartTime);
            modelBuilder.Entity<SystemStateLog>().HasIndex(s => s.StartTime);
            modelBuilder.Entity<Score>().HasIndex(s => s.Time);
        }
    }
}
