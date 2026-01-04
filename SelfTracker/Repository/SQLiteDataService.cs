using System;
using System.Data.SQLite;
using System.IO;

namespace SelfTracker.Repository
{
    public class SQLiteDataService
    {
        private string _connectionString;
        public string ConnectionString => _connectionString;

        public SQLiteDataService()
        {
            // 1. 确定数据库路径
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "quantify.sqlite");

            // 2. 确保 Data 文件夹存在
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={dbPath};Version=3;";

            // 3. 初始化数据库和升级
            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // 3.1 创建基础表
            CreateBaseTables(connection);

            // 3.2 创建 schema_version 表并初始化
            EnsureSchemaVersionTable(connection);

            // 3.3 获取当前版本并执行升级
            int currentVersion = GetCurrentSchemaVersion(connection);
            UpgradeSchema(connection, currentVersion);
        }

        private void CreateBaseTables(SQLiteConnection connection)
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS productivity_counts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp DATETIME DEFAULT (datetime('now', 'localtime')),
                keystrokes INTEGER DEFAULT 0,
                copy_count INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS activity_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                process_name TEXT,
                window_title TEXT,
                start_time DATETIME NOT NULL,
                end_time DATETIME NOT NULL,
                duration INTEGER
            );

            CREATE TABLE IF NOT EXISTS afk_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                start_time DATETIME NOT NULL,
                end_time DATETIME NOT NULL
            );

            CREATE TABLE IF NOT EXISTS system_sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                start_time DATETIME NOT NULL,
                end_time DATETIME
            );

            CREATE INDEX IF NOT EXISTS idx_prod_time ON productivity_counts(timestamp);
            CREATE INDEX IF NOT EXISTS idx_act_start ON activity_logs(start_time);
            CREATE INDEX IF NOT EXISTS idx_afk_start ON afk_logs(start_time);
            CREATE INDEX IF NOT EXISTS idx_session_start ON system_sessions(start_time);
            ";

            using var cmd = new SQLiteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        private void EnsureSchemaVersionTable(SQLiteConnection connection)
        {
            string sql = @"
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER NOT NULL
            );

            INSERT INTO schema_version (version)
            SELECT 1
            WHERE NOT EXISTS (SELECT 1 FROM schema_version);
            ";
            using var cmd = new SQLiteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        private int GetCurrentSchemaVersion(SQLiteConnection connection)
        {
            using var cmd = new SQLiteCommand("SELECT version FROM schema_version LIMIT 1", connection);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private void SetSchemaVersion(SQLiteConnection connection, int version)
        {
            using var cmd = new SQLiteCommand("UPDATE schema_version SET version = @v", connection);
            cmd.Parameters.AddWithValue("@v", version);
            cmd.ExecuteNonQuery();
        }

        private void UpgradeSchema(SQLiteConnection connection, int currentVersion)
        {
            // 这里是示例，假设 v2 增加了 session_id 和 activity_type
            if (currentVersion < 2)
            {
                UpgradeToV2(connection);
                SetSchemaVersion(connection, 2);
            }

            if (currentVersion < 3)
            {
                UpgradeToV3(connection);
                SetSchemaVersion(connection, 3);
            }
            // --- 新增 V4 逻辑 ---
            if (currentVersion < 4)
            {
                UpgradeToV4(connection);
                SetSchemaVersion(connection, 4);
            }
        }

        private void UpgradeToV2(SQLiteConnection connection)
        {
            string sql = @"
            -- 给行为表加 session_id
            ALTER TABLE productivity_counts ADD COLUMN session_id INTEGER;
            ALTER TABLE activity_logs ADD COLUMN session_id INTEGER;
            ALTER TABLE afk_logs ADD COLUMN session_id INTEGER;

            -- activity 语义字段
            ALTER TABLE activity_logs ADD COLUMN activity_type TEXT;

            -- 明确生产力统计周期
            ALTER TABLE productivity_counts ADD COLUMN period_start DATETIME;
            ALTER TABLE productivity_counts ADD COLUMN period_seconds INTEGER DEFAULT 60;
            ";
            using var cmd = new SQLiteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        private void UpgradeToV3(SQLiteConnection connection)
        {
            string sql = @"
    -- 创建分类规则表
    CREATE TABLE IF NOT EXISTS category_rules (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        process_name TEXT UNIQUE NOT NULL, -- 进程名，设为唯一索引，方便更新
        activity_type TEXT NOT NULL        -- 对应的标签名称
    );

    -- 插入一些初始默认规则（可选）
    INSERT OR IGNORE INTO category_rules (process_name, activity_type) VALUES ('devenv', '工作');
    INSERT OR IGNORE INTO category_rules (process_name, activity_type) VALUES ('chrome', '社交');
    ";
            using var cmd = new SQLiteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }
        private void UpgradeToV4(SQLiteConnection connection)
        {
            string sql = @"
    -- 1. 创建标签定义表
    CREATE TABLE IF NOT EXISTS category_definitions (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT UNIQUE NOT NULL
    );

    -- 2. 插入初始化标签（这样页面打开时不会是空的）
    INSERT OR IGNORE INTO category_definitions (name) VALUES ('编程开发');

    INSERT OR IGNORE INTO category_definitions (name) VALUES ('游戏');

    INSERT OR IGNORE INTO category_definitions (name) VALUES ('社交');

    INSERT OR IGNORE INTO category_definitions (name) VALUES ('学习');

    INSERT OR IGNORE INTO category_definitions (name) VALUES ('系统维护');
    ";
            using var cmd = new SQLiteCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }
    }
}
