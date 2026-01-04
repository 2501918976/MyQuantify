using SelfTracker.Entity;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SelfTracker.Repository
{
    /// <summary>
    /// 数据仓库类：负责所有与 SQLite 数据库的交互逻辑
    /// </summary>
    public class DataRepository
    {
        private readonly string _connectionString;

        public DataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region 1. 数据写入记录 (Command - 写入器)
        // 此区域包含所有将采集到的原始数据持久化到数据库的方法

        /// <summary>
        /// 记录生产力增量（按键数和复制数）
        /// </summary>
        public void InsertProductivity(ProductivityCount record)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = @"INSERT INTO productivity_counts 
                                (keystrokes, copy_count, session_id, period_start, period_seconds) 
                                VALUES (@keys, @copies, @sid, @pStart, @pSec)";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@keys", record.Keystrokes);
            cmd.Parameters.AddWithValue("@copies", record.CopyCount);
            cmd.Parameters.AddWithValue("@sid", record.SessionId);
            cmd.Parameters.AddWithValue("@pStart", record.PeriodStart ?? DateTime.Now);
            cmd.Parameters.AddWithValue("@pSec", record.PeriodSeconds);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 记录一段连续的窗口活动（软件使用记录）
        /// </summary>
        public void InsertActivity(ActivityLog log)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = @"INSERT INTO activity_logs 
                                (process_name, window_title, start_time, end_time, duration, session_id, activity_type) 
                                VALUES (@pName, @wTitle, @start, @end, @dur, @sid, @type)";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pName", log.ProcessName);
            cmd.Parameters.AddWithValue("@wTitle", log.WindowTitle);
            cmd.Parameters.AddWithValue("@start", log.StartTime);
            cmd.Parameters.AddWithValue("@end", log.EndTime);
            cmd.Parameters.AddWithValue("@dur", log.Duration);
            cmd.Parameters.AddWithValue("@sid", log.SessionId);
            cmd.Parameters.AddWithValue("@type", log.ActivityType ?? "General");
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 记录一段挂机时间
        /// </summary>
        public void InsertAfk(AfkLog log)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "INSERT INTO afk_logs (start_time, end_time, session_id) VALUES (@start, @end, @sid)";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@start", log.StartTime);
            cmd.Parameters.AddWithValue("@end", log.EndTime);
            cmd.Parameters.AddWithValue("@sid", log.SessionId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 开启一个新的监控会话，并返回会话 ID
        /// </summary>
        public long StartSession()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "INSERT INTO system_sessions (start_time) VALUES (datetime('now', 'localtime')); SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn);
            return (long)cmd.ExecuteScalar();
        }

        /// <summary>
        /// 更新会话的结束时间（程序正常关闭时调用）
        /// </summary>
        public void UpdateSessionEnd(long sessionId)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "UPDATE system_sessions SET end_time = datetime('now', 'localtime') WHERE id = @id";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", sessionId);
            cmd.ExecuteNonQuery();
        }
        #endregion

        #region 2. 基础单项查询 (Basic Queries - 单条或即时数据)
        // 此区域包含用于 UI 实时显示的简单查询逻辑

        /// <summary>
        /// 获取今日累计击键次数
        /// </summary>
        public int GetTodayTotalKeystrokes()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "SELECT SUM(keystrokes) FROM productivity_counts WHERE date(timestamp, 'localtime') = date('now', 'localtime')";
            using var cmd = new SQLiteCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        /// <summary>
        /// 获取今日累计挂机时长（秒）
        /// </summary>
        public int GetTodayAfkDurationSeconds()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = @"SELECT SUM(strftime('%s', end_time) - strftime('%s', start_time)) 
                               FROM afk_logs 
                               WHERE date(start_time, 'localtime') = date('now', 'localtime')";
            using var cmd = new SQLiteCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        /// <summary>
        /// 获取今日累计复制次数
        /// </summary>
        public int GetTodayTotalCopyCount()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "SELECT SUM(copy_count) FROM productivity_counts WHERE date(timestamp, 'localtime') = date('now', 'localtime')";
            using var cmd = new SQLiteCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        /// <summary>
        /// 获取今日活跃时长（秒）
        /// </summary>
        public int GetTodayActiveDurationSeconds()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "SELECT SUM(duration) FROM activity_logs WHERE date(start_time, 'localtime') = date('now', 'localtime')";
            using var cmd = new SQLiteCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        /// <summary>
        /// 获取最近一次记录的活动信息（用于 UI 状态回显）
        /// </summary>
        public ActivityLog GetLatestActivity()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "SELECT * FROM activity_logs ORDER BY id DESC LIMIT 1";
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new ActivityLog
                {
                    ProcessName = reader["process_name"].ToString(),
                    WindowTitle = reader["window_title"].ToString(),
                    ActivityType = reader["activity_type"].ToString()
                };
            }
            return null;
        }
        #endregion

        #region 3. 聚合查询统计 (Aggregated Queries - 用于报表生成)
        // 预留区域：未来将在此处编写用于可视化报表（ECharts）的数据聚合 SQL 逻辑
        // 例如：按天分组的击键趋势、软件使用时长 Top 10 等
        #endregion


        #region 4. 分类管理 (Category Management)

        /// <summary>
        /// 获取所有在 activity_logs 中出现过，但在规则库中尚未分类的进程名
        /// </summary>
        public List<string> GetUncategorizedProcesses()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            // 找出所有在活动日志中存在，但不在 category_rules 表中的进程
            const string sql = @"SELECT DISTINCT process_name FROM activity_logs 
                        WHERE activity_type = 'General' 
                        AND process_name NOT IN (SELECT process_name FROM category_rules)";
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            var list = new List<string>();
            while (reader.Read()) list.Add(reader[0].ToString());
            return list;
        }

        /// <summary>
        /// 添加或更新一条分类规则，并同步更新历史数据
        /// </summary>
        public void UpdateCategoryAndApplyToHistory(string processName, string newType)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // 1. 插入规则表 (使用 REPLACE 语法，如果存在则更新)
                const string sqlRule = "INSERT OR REPLACE INTO category_rules (process_name, activity_type) VALUES (@name, @type)";
                using var cmd1 = new SQLiteCommand(sqlRule, conn);
                cmd1.Parameters.AddWithValue("@name", processName);
                cmd1.Parameters.AddWithValue("@type", newType);
                cmd1.ExecuteNonQuery();

                // 2. 核心：同步更新 activity_logs 中所有同进程的记录（可选）
                const string sqlHistory = "UPDATE activity_logs SET activity_type = @type WHERE process_name = @name";
                using var cmd2 = new SQLiteCommand(sqlHistory, conn);
                cmd2.Parameters.AddWithValue("@name", processName);
                cmd2.Parameters.AddWithValue("@type", newType);
                cmd2.ExecuteNonQuery();

                trans.Commit();
            }
            catch { trans.Rollback(); throw; }
        }

        /// <summary>
        /// 获取内存缓存用的所有规则映射
        /// </summary>
        public Dictionary<string, string> GetAllCategoryRules()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "SELECT process_name, activity_type FROM category_rules";
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read()) dict[reader[0].ToString()] = reader[1].ToString();
            return dict;
        }
        #endregion


        #region 进程分类的临时代码
        // 获取所有已记录的进程及其当前的分类
        public List<ProcessCategoryInfo> GetProcessCategories()
        {
            var list = new List<ProcessCategoryInfo>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // 逻辑：从 activity_logs 提取唯一的 process_name 及其对应的 activity_type
            // 使用 MAX(activity_type) 是为了拿到最新的分类值（如果有的话）
            string sql = @"
        SELECT process_name, MAX(activity_type) as category 
        FROM activity_logs 
        WHERE process_name IS NOT NULL AND process_name != 'Idle'
        GROUP BY process_name 
        ORDER BY duration DESC";

            using var cmd = new SQLiteCommand(sql, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ProcessCategoryInfo
                {
                    ProcessName = reader["process_name"].ToString(),
                    Category = reader["category"]?.ToString() ?? "未分类"
                });
            }
            return list;
        }

        // 更新某个进程的分类
        public void UpdateProcessCategory(string processName, string newCategory)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            string sql = "UPDATE activity_logs SET activity_type = @category WHERE process_name = @name";
            using var cmd = new SQLiteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@category", newCategory);
            cmd.Parameters.AddWithValue("@name", processName);
            cmd.ExecuteNonQuery();
        }

        // 辅助类
        public class ProcessCategoryInfo
        {
            public string ProcessName { get; set; }
            public string Category { get; set; }
        }
        #endregion

        #region 4. 深度分类管理 (Advanced Category Management)

        // --- 4.1 标签定义管理 (增删改查用户自定义的标签) ---

        /// <summary>
        /// 获取所有预定义的分类标签 (从数据库读取)
        /// </summary>
        public List<string> GetAllCategoryDefinitions()
        {
            var list = new List<string>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "SELECT name FROM category_definitions ORDER BY id ASC";
            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(reader["name"].ToString());

            // 如果数据库是空的，返回一些默认值
            if (list.Count == 0) return new List<string> { "工作", "娱乐", "社交", "学习", "系统" };
            return list;
        }

        /// <summary>
        /// 新增一个分类标签
        /// </summary>
        public void AddCategoryDefinition(string categoryName)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = "INSERT OR IGNORE INTO category_definitions (name) VALUES (@name)";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", categoryName);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 删除一个分类标签
        /// </summary>
        public void DeleteCategoryDefinition(string categoryName)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            // 注意：删除标签时，最好把已关联该标签的规则重置为 '未分类'
            const string sql = @"
        DELETE FROM category_definitions WHERE name = @name;
        UPDATE category_rules SET activity_type = '未分类' WHERE activity_type = @name;
        UPDATE activity_logs SET activity_type = '未分类' WHERE activity_type = @name;";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", categoryName);
            cmd.ExecuteNonQuery();
        }

        // --- 4.2 程序分类逻辑 (分类与重新分类) ---

        /// <summary>
        /// 获取所有进程及其分类状态（包括已分类和未分类）
        /// 这个方法用于填充左侧列表
        /// </summary>
        public List<ProcessCategoryInfo> GetFullProcessCategoryMap()
        {
            var list = new List<ProcessCategoryInfo>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            // 使用 LEFT JOIN 结合活动日志和规则表
            // 这样既能看到还没分类的进程，也能看到已经分类的进程
            const string sql = @"
        SELECT DISTINCT a.process_name, 
               COALESCE(r.activity_type, '未分类') as category
        FROM activity_logs a
        LEFT JOIN category_rules r ON a.process_name = r.process_name
        WHERE a.process_name IS NOT NULL AND a.process_name != 'Idle'
        ORDER BY category ASC, a.process_name ASC";

            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ProcessCategoryInfo
                {
                    ProcessName = reader["process_name"].ToString(),
                    Category = reader["category"].ToString()
                });
            }
            return list;
        }

        /// <summary>
        /// 【核心方法】设置进程分类：存入规则表，并追溯更新历史记录
        /// </summary>
        /// <param name="processName">进程名</param>
        /// <param name="newCategory">新标签名</param>
        public void ApplyProcessCategory(string processName, string newCategory)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // 1. 更新或插入规则表 (决定未来的分类)
                const string sqlUpsert = @"
            INSERT INTO category_rules (process_name, activity_type) 
            VALUES (@name, @type)
            ON CONFLICT(process_name) DO UPDATE SET activity_type = @type;";
                using var cmd1 = new SQLiteCommand(sqlUpsert, conn);
                cmd1.Parameters.AddWithValue("@name", processName);
                cmd1.Parameters.AddWithValue("@type", newCategory);
                cmd1.ExecuteNonQuery();

                // 2. 追溯更新历史记录 (更新过去的分类数据)
                const string sqlHistory = "UPDATE activity_logs SET activity_type = @type WHERE process_name = @name";
                using var cmd2 = new SQLiteCommand(sqlHistory, conn);
                cmd2.Parameters.AddWithValue("@name", processName);
                cmd2.Parameters.AddWithValue("@type", newCategory);
                cmd2.ExecuteNonQuery();

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 重置某个进程的分类（即删除规则，回归“未分类”）
        /// </summary>
        public void ResetProcessCategory(string processName)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            const string sql = @"
        DELETE FROM category_rules WHERE process_name = @name;
        UPDATE activity_logs SET activity_type = '未分类' WHERE process_name = @name;";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", processName);
            cmd.ExecuteNonQuery();
        }

        #endregion
    }
}