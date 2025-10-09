using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.IO;
using System.Data.SQLite;

namespace MyQuantifyApp.Database
{
    public class SQLiteDataService
    {
        private string _connectionString;
        public string ConnectionString => _connectionString;

        public SQLiteDataService()
        {
            // 数据库文件路径
            string dbPath = @"C:\Users\admin\source\repos\MyQuantify\MyQuantifyApp\Logs\quantify.sqlite";

            // 1️⃣ 确保目录存在
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={dbPath};Version=3;";

            // 2️⃣ 如果数据库文件不存在，创建并初始化表结构
            if (!File.Exists(dbPath))
            {
                Console.WriteLine("检测到数据库文件不存在，正在创建并初始化...");
                SQLiteConnection.CreateFile(dbPath);
                InitializeDatabase(); // 调用你已有的建表逻辑
            }
        }

        public void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 开启外键支持
                using (var command = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    command.ExecuteNonQuery();
                }

                // 1️⃣ Categories 表 —— 分类标签
                // 由于需要手动指定 ID=1，我们让 Id 保持 INTEGER PRIMARY KEY，SQLite会自动处理 AUTOINCREMENT
                string createCategoriesTableSql = @"
CREATE TABLE IF NOT EXISTS Categories (
    Id INTEGER PRIMARY KEY, -- 移除 AUTOINCREMENT 以便手动插入 ID=1
    Name TEXT NOT NULL UNIQUE,
    Description TEXT
);";
                new SQLiteCommand(createCategoriesTableSql, connection).ExecuteNonQuery();
                // ⚠️ 注意：SQLite 的 INTEGER PRIMARY KEY 本身就是 RowId 的别名，默认会自增。
                // 如果想要强制 ID=1 成功，并且后续从 2 开始自增，最好的方式是手动插入后，
                // 调整 SQLite 的自增计数器。但对于 SQLite，最可靠的做法是手动插入。
                // 为了兼容性，我们暂时保持原样，并使用 REPLACE INTO 或 INSERT OR IGNORE。

                // ────────────────────────────────
                // 【新增逻辑】: 强制插入 ID=1 的“未分类”标签
                string insertUncategorizedSql = @"
INSERT OR IGNORE INTO Categories (Id, Name, Description)
VALUES (1, '未分类', '系统默认标签，用于标识未归属的进程。');
";
                new SQLiteCommand(insertUncategorizedSql, connection).ExecuteNonQuery();


                // ────────────────────────────────
                // 2️⃣ Processes 表 —— 进程信息
                string createProcessesTableSql = @"
CREATE TABLE IF NOT EXISTS Processes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProcessName TEXT NOT NULL,
    FilePath TEXT,
    CategoryId INTEGER,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL,
    UNIQUE (ProcessName, FilePath)
);";
                new SQLiteCommand(createProcessesTableSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 3️⃣ Windows 表 —— 窗口记录
                string createWindowsTableSql = @"
CREATE TABLE IF NOT EXISTS Windows (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProcessId INTEGER NOT NULL,
    WindowTitle TEXT NOT NULL,
    CategoryId INTEGER,
    FOREIGN KEY (ProcessId) REFERENCES Processes(Id) ON DELETE CASCADE,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL,
    UNIQUE (ProcessId, WindowTitle)
);";
                new SQLiteCommand(createWindowsTableSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 4️⃣ WindowActivities 表 —— 窗口活动时间记录
                string createWindowActivitiesTableSql = @"
CREATE TABLE IF NOT EXISTS WindowActivities (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    WindowId INTEGER NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME,
    DurationSeconds INTEGER,
    FOREIGN KEY (WindowId) REFERENCES Windows(Id) ON DELETE CASCADE
);";
                new SQLiteCommand(createWindowActivitiesTableSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 5️⃣ KeyLogs 表 —— 键盘输入日志（逐条日志）
                string createKeyTableSql = @"
CREATE TABLE IF NOT EXISTS KeyLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    WindowActivityId INTEGER,
    KeyChar TEXT NOT NULL,
    Timestamp DATETIME NOT NULL,
    FOREIGN KEY (WindowActivityId) REFERENCES WindowActivities(Id) ON DELETE CASCADE
);";
                new SQLiteCommand(createKeyTableSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 6️⃣ ClipboardLogs 表 —— 剪贴板记录（逐条）
                string createClipboardTableSql = @"
CREATE TABLE IF NOT EXISTS ClipboardLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    WindowActivityId INTEGER,
    Content TEXT,
    Length INTEGER NOT NULL,
    Timestamp DATETIME NOT NULL,
    FOREIGN KEY (WindowActivityId) REFERENCES WindowActivities(Id) ON DELETE CASCADE
);";
                new SQLiteCommand(createClipboardTableSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 7️⃣ AfkLogs 表 —— 离开键盘记录
                string createAfkTableSql = @"
CREATE TABLE IF NOT EXISTS AfkLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    DurationSeconds INTEGER NOT NULL
);";
                new SQLiteCommand(createAfkTableSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 8️⃣ DailySummary 表 —— 每日汇总统计
                string createDailySummarySql = @"
CREATE TABLE IF NOT EXISTS DailySummary (
    Date TEXT PRIMARY KEY,
    KeyCount INTEGER DEFAULT 0,
    CopyCount INTEGER DEFAULT 0,
    AfkSeconds INTEGER DEFAULT 0,
    WorkSeconds INTEGER DEFAULT 0,
    GameSeconds INTEGER DEFAULT 0,
    TotalActiveSeconds INTEGER DEFAULT 0
);";
                new SQLiteCommand(createDailySummarySql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 9️⃣ KeyAggregates 表 —— 每日按键聚合（每天每个按键的计数）
                string createKeyAggregatesSql = @"
CREATE TABLE IF NOT EXISTS KeyAggregates (
    Date TEXT NOT NULL,
    KeyChar TEXT NOT NULL,
    Count INTEGER DEFAULT 0,
    PRIMARY KEY (Date, KeyChar)
);";
                new SQLiteCommand(createKeyAggregatesSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 🔟 ProcessTimeStats 表 —— 每日每个进程使用时间（由 WindowActivities 聚合生成）
                string createProcessTimeStatsSql = @"
CREATE TABLE IF NOT EXISTS ProcessTimeStats (
    Date TEXT NOT NULL,
    ProcessId INTEGER NOT NULL,
    ProcessName TEXT NOT NULL,
    ActiveSeconds INTEGER DEFAULT 0,
    PRIMARY KEY (Date, ProcessId),
    FOREIGN KEY (ProcessId) REFERENCES Processes(Id) ON DELETE CASCADE
);";
                new SQLiteCommand(createProcessTimeStatsSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 11️⃣ CategoryTimeStats 表 —— 分类活动时长（按日、按分类，作为缓存/加速用）
                string createCategoryTimeStatsSql = @"
CREATE TABLE IF NOT EXISTS CategoryTimeStats (
    Date TEXT NOT NULL,
    CategoryId INTEGER NOT NULL,
    Name TEXT NOT NULL,  -- ⚠️ 新增：分类名称，用于提高查询效率
    ActiveSeconds INTEGER DEFAULT 0,
    PRIMARY KEY (Date, CategoryId),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);";
                new SQLiteCommand(createCategoryTimeStatsSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 12️⃣ SQL 视图：v_CategoryDailySummary —— 直接展示 CategoryTimeStats 的结果
                string createViewCategoryDailySql = @"
CREATE VIEW IF NOT EXISTS v_CategoryDailySummary AS
SELECT 
    c.Name AS CategoryName,
    s.Date,
    s.ActiveSeconds
FROM CategoryTimeStats s
JOIN Categories c ON s.CategoryId = c.Id;
";
                new SQLiteCommand(createViewCategoryDailySql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 13️⃣ SQL 视图：v_CategoryTimeSummary —— 通过 ProcessTimeStats 聚合并按分类汇总（方便按进程来源统计）
                string createViewCategoryFromProcessSql = @"
CREATE VIEW IF NOT EXISTS v_CategoryTimeSummary AS
SELECT 
    c.Id AS CategoryId,
    c.Name AS CategoryName,
    p.Date,
    SUM(p.ActiveSeconds) AS TotalActiveSeconds
FROM ProcessTimeStats p
JOIN Processes pr ON p.ProcessId = pr.Id
JOIN Categories c ON pr.CategoryId = c.Id
GROUP BY c.Id, p.Date;
";
                new SQLiteCommand(createViewCategoryFromProcessSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 14. 专注会话记录表
                // ────────────────────────────────
                // 功能：
                //  - Title：任务名称
                //  - StartTime / EndTime：开始与结束时间
                //  - FocusDuration / RestDuration：本次设定的专注时间与休息时间（分钟）
                //  - Status：枚举值（Running, Paused, Completed, Canceled）
                //  - IsCompleted：是否完成
                string createFocusSessionSql = @"
                CREATE TABLE IF NOT EXISTS FocusSession (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    StartTime DATETIME NULL,
                    EndTime DATETIME NULL,
                    FocusDuration INTEGER DEFAULT 25,
                    RestDuration INTEGER DEFAULT 5,
                    Status TEXT DEFAULT 'Idle',
                    IsCompleted INTEGER DEFAULT 0
                );
            ";
                new SQLiteCommand(createFocusSessionSql, connection).ExecuteNonQuery();

                // ────────────────────────────────
                // 🔧 索引优化（包括 KeyAggregates、ProcessTimeStats、CategoryTimeStats 等）
                string createIndexesSql = @"

CREATE INDEX IF NOT EXISTS idx_keylogs_timestamp ON KeyLogs(Timestamp);
CREATE INDEX IF NOT EXISTS idx_clipboardlogs_timestamp ON ClipboardLogs(Timestamp);
CREATE INDEX IF NOT EXISTS idx_afklogs_starttime ON AfkLogs(StartTime);
CREATE INDEX IF NOT EXISTS idx_windowactivities_starttime ON WindowActivities(StartTime);


CREATE INDEX IF NOT EXISTS idx_keyaggregates_date ON KeyAggregates(Date);

CREATE INDEX IF NOT EXISTS idx_windowactivities_windowid ON WindowActivities(WindowId);
CREATE INDEX IF NOT EXISTS idx_windows_processid ON Windows(ProcessId);
CREATE INDEX IF NOT EXISTS idx_processes_categoryid ON Processes(CategoryId);
CREATE INDEX IF NOT EXISTS idx_windows_categoryid ON Windows(CategoryId);

CREATE INDEX IF NOT EXISTS idx_processtimestats_date ON ProcessTimeStats(Date);
CREATE INDEX IF NOT EXISTS idx_categorytimestats_date ON CategoryTimeStats(Date);

CREATE INDEX IF NOT EXISTS idx_keylogs_windowactivityid ON KeyLogs(WindowActivityId);
CREATE INDEX IF NOT EXISTS idx_clipboardlogs_windowactivityid ON ClipboardLogs(WindowActivityId);
";
                new SQLiteCommand(createIndexesSql, connection).ExecuteNonQuery();

                connection.Close();
            }
        }
    }
}