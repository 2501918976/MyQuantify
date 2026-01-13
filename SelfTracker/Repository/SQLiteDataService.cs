using System;
using System.Data.SQLite;
using System.IO;

namespace SelfTracker.Repository
{
    public class SQLiteDataService
    {
        private readonly string _connectionString;

        public string ConnectionString => _connectionString;

        public SQLiteDataService()
        {
            // 数据库文件路径
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "quantify.sqlite");

            // 确保 Data 文件夹存在
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={dbPath};Version=3;";

            // 初始化数据库
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            CreateTables(connection);
            EnsureSchemaVersionTable(connection);
            int currentVersion = GetCurrentSchemaVersion(connection);
            UpgradeSchema(connection, currentVersion);
        }

        #region 创建表

        private void CreateTables(SQLiteConnection connection)
        {
            string sql = @"
            -- 系统状态表
            CREATE TABLE IF NOT EXISTS SystemStateLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionKey TEXT,
                Type INTEGER NOT NULL,
                StartTime DATETIME NOT NULL,
                EndTime DATETIME,
                Duration INTEGER,
                Location TEXT,
                DeviceName TEXT
            );

            -- 进程表
            CREATE TABLE IF NOT EXISTS ProcessInfos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessName TEXT NOT NULL,
                CategoryId INTEGER,
                FOREIGN KEY(CategoryId) REFERENCES Categories(Id)
            );

            -- 分类表
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                ColorCode TEXT,
                LastModifiedTime DATETIME
            );

            -- 分类规则表
            CREATE TABLE IF NOT EXISTS CategoryRules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryId INTEGER NOT NULL,
                RuleType INTEGER NOT NULL,
                MatchValue TEXT NOT NULL,
                Priority INTEGER NOT NULL,
                FOREIGN KEY(CategoryId) REFERENCES Categories(Id)
            );

            -- 活动日志表
            CREATE TABLE IF NOT EXISTS ActivityLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessInfoId INTEGER NOT NULL,
                SystemStateLogId INTEGER NOT NULL,
                WindowTitle TEXT,
                StartTime DATETIME NOT NULL,
                EndTime DATETIME NOT NULL,
                Duration INTEGER,
                FOREIGN KEY(ProcessInfoId) REFERENCES ProcessInfos(Id),
                FOREIGN KEY(SystemStateLogId) REFERENCES SystemStateLogs(Id)
            );

            -- 打字日志表
            CREATE TABLE IF NOT EXISTS TypingLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessInfoId INTEGER NOT NULL,
                SystemStateLogId INTEGER NOT NULL,
                KeyCount INTEGER NOT NULL,
                StartTime DATETIME NOT NULL,
                EndTime DATETIME NOT NULL,
                Duration INTEGER,
                FOREIGN KEY(ProcessInfoId) REFERENCES ProcessInfos(Id),
                FOREIGN KEY(SystemStateLogId) REFERENCES SystemStateLogs(Id)
            );

            -- 复制日志表
            CREATE TABLE IF NOT EXISTS CopyLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessInfoId INTEGER,
                SystemStateLogId INTEGER NOT NULL,
                CopyCount INTEGER NOT NULL,
                StartTime DATETIME NOT NULL,
                EndTime DATETIME NOT NULL,
                Duration INTEGER,
                FOREIGN KEY(ProcessInfoId) REFERENCES ProcessInfos(Id),
                FOREIGN KEY(SystemStateLogId) REFERENCES SystemStateLogs(Id)
            );

            -- 修改后的 Scores 表定义
            CREATE TABLE IF NOT EXISTS Scores (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Time DATETIME NOT NULL,
                EfficiencyScore INTEGER NOT NULL,
                LastUpdated DATETIME NOT NULL
            );

            -- 索引
            CREATE INDEX IF NOT EXISTS idx_ActivityLogs_StartTime ON ActivityLogs(StartTime);
            CREATE INDEX IF NOT EXISTS idx_TypingLogs_StartTime ON TypingLogs(StartTime);
            CREATE INDEX IF NOT EXISTS idx_CopyLogs_StartTime ON CopyLogs(StartTime);
            CREATE INDEX IF NOT EXISTS idx_SystemStateLogs_StartTime ON SystemStateLogs(StartTime);
            CREATE INDEX IF NOT EXISTS idx_Scores_Time ON Scores(Time);
            ";
            using var cmd = new SQLiteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Schema 版本管理

        private void EnsureSchemaVersionTable(SQLiteConnection connection)
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS SchemaVersion (
                Version INTEGER NOT NULL
            );

            INSERT INTO SchemaVersion (Version)
            SELECT 1
            WHERE NOT EXISTS (SELECT 1 FROM SchemaVersion);
            ";
            using var cmd = new SQLiteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        private int GetCurrentSchemaVersion(SQLiteConnection connection)
        {
            using var cmd = new SQLiteCommand("SELECT Version FROM SchemaVersion LIMIT 1", connection);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private void SetSchemaVersion(SQLiteConnection connection, int version)
        {
            using var cmd = new SQLiteCommand("UPDATE SchemaVersion SET Version = @v", connection);
            cmd.Parameters.AddWithValue("@v", version);
            cmd.ExecuteNonQuery();
        }

        // 修改 UpgradeSchema 逻辑
        private void UpgradeSchema(SQLiteConnection connection, int currentVersion)
        {
            if (currentVersion < 2)
            {
                // 执行 V2 升级：创建 Scores 表
                string upgradeSql = @"
        CREATE TABLE IF NOT EXISTS Scores (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Time DATETIME NOT NULL,
            EfficiencyScore INTEGER NOT NULL,
            LastUpdated DATETIME NOT NULL
        );";

                using (var cmd = new SQLiteCommand(upgradeSql, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 升级版本号
                SetSchemaVersion(connection, 2);
            }
        }

        #endregion
    }
}
