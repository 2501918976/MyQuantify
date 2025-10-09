using System;
using System.Data.SQLite;
using System.Globalization;

namespace MyQuantifyApp.Services
{

    public class AggregationService
    {
        private readonly string _connectionString;

        public AggregationService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void AggregateAll(DateTime date)
        {
            // 在事务中计算好日期参数，避免在每个子方法中重复计算
            string dateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string dateFormat = "yyyy-MM-dd";

            // 1. 用于 SQL WHERE 子句的时间范围参数 (包含时间部分)
            string startDateStr = date.ToString(dateTimeFormat, CultureInfo.InvariantCulture);
            string endDateStr = date.AddDays(1).ToString(dateTimeFormat, CultureInfo.InvariantCulture);

            // 2. 用于聚合表 (KeyAggregates, DailySummary 等) Date 列的日期参数 (仅日期部分)
            string dateOnlyStr = date.ToString(dateFormat, CultureInfo.InvariantCulture);


            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            // 开始事务
            using var transaction = conn.BeginTransaction();
            try
            {
                // 将所有需要的参数传递给子方法
                AggregateKeyAggregates(dateOnlyStr, startDateStr, endDateStr, conn, transaction);
                AggregateProcessTimeStats(dateOnlyStr, startDateStr, endDateStr, conn, transaction);
                AggregateCategoryTimeStats(dateOnlyStr, startDateStr, endDateStr, conn, transaction);
                AggregateDailySummary(dateOnlyStr, startDateStr, endDateStr, conn, transaction);

                transaction.Commit(); // 提交所有更改
            }
            catch (Exception ex)
            {
                // 如果任一聚合失败，回滚所有更改
                transaction.Rollback();
                // 抛出异常或记录日志
                Console.WriteLine($"聚合失败并回滚：{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 聚合每日按键统计。
        /// </summary>
        // 更改方法签名，接受 DateOnlyStr
        private void AggregateKeyAggregates(string dateOnlyStr, string startDateStr, string endDateStr, SQLiteConnection conn, SQLiteTransaction transaction)
        {
            // 【优化点 3：UPSERT】使用 ON CONFLICT DO UPDATE 替代 INSERT OR REPLACE
            // 【优化点 2：时间筛选】使用范围筛选 WHERE Timestamp >= @Start AND Timestamp < @End
            string sql = @"
                INSERT INTO KeyAggregates (Date, KeyChar, Count)
                SELECT
                    @DateOnlyStr AS Date, 
                    k.KeyChar,
                    COUNT(*) AS Count
                FROM KeyLogs k
                WHERE k.Timestamp >= @StartDateStr AND k.Timestamp < @EndDateStr
                GROUP BY k.KeyChar
                ON CONFLICT(Date, KeyChar) DO UPDATE SET
                    Count = excluded.Count;
            ";

            using var cmd = new SQLiteCommand(sql, conn, transaction);
            // 🔑 修正：使用 dateOnlyStr 绑定聚合表的主键 Date
            cmd.Parameters.AddWithValue("@DateOnlyStr", dateOnlyStr);
            cmd.Parameters.AddWithValue("@StartDateStr", startDateStr);
            cmd.Parameters.AddWithValue("@EndDateStr", endDateStr);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 聚合每日进程时间统计。
        /// </summary>

        private void AggregateProcessTimeStats(string dateOnlyStr, string startDateStr, string endDateStr, SQLiteConnection conn, SQLiteTransaction transaction)
        {
            string sql = @"
        INSERT INTO ProcessTimeStats (Date, ProcessId, ProcessName, ActiveSeconds) -- ⚠️ 添加 ProcessName
        SELECT
            @DateOnlyStr AS Date,
            w.ProcessId,
            p.ProcessName, -- ⚠️ 从 Processes 表获取 ProcessName
            SUM(wa.DurationSeconds) AS ActiveSeconds
        FROM WindowActivities wa
        JOIN Windows w ON wa.WindowId = w.Id
        JOIN Processes p ON w.ProcessId = p.Id -- ⚠️ 新增 JOIN 到 Processes 表
        WHERE wa.StartTime >= @StartDateStr AND wa.StartTime < @EndDateStr
        GROUP BY w.ProcessId, p.ProcessName -- ⚠️ GROUP BY 也要包含 ProcessName (尽管 ProcessId 已保证唯一性，但规范起见)
        ON CONFLICT(Date, ProcessId) DO UPDATE SET
            ActiveSeconds = excluded.ActiveSeconds,
            ProcessName = excluded.ProcessName; -- ⚠️ 如果 Processes 表中的名称被更新，这里也更新（虽然 ProcessName 通常不变）
    ";

            using var cmd = new SQLiteCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@DateOnlyStr", dateOnlyStr);
            cmd.Parameters.AddWithValue("@StartDateStr", startDateStr);
            cmd.Parameters.AddWithValue("@EndDateStr", endDateStr);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 聚合每日分类时间统计。
        /// </summary>

        private void AggregateCategoryTimeStats(string dateOnlyStr, string startDateStr, string endDateStr, SQLiteConnection conn, SQLiteTransaction transaction)
        {
            string sql = @"
        INSERT INTO CategoryTimeStats (Date, CategoryId, Name, ActiveSeconds)
        SELECT
            @DateOnlyStr AS Date,
            w.CategoryId,
            c.Name,
            SUM(wa.DurationSeconds) AS ActiveSeconds
        FROM WindowActivities wa
        JOIN Windows w ON wa.WindowId = w.Id
        JOIN Categories c ON w.CategoryId = c.Id
        WHERE w.CategoryId IS NOT NULL AND wa.StartTime >= @StartDateStr AND wa.StartTime < @EndDateStr
        GROUP BY w.CategoryId, c.Name
        ON CONFLICT(Date, CategoryId) DO UPDATE SET
            ActiveSeconds = excluded.ActiveSeconds,
            Name = excluded.Name;
    ";

            using var cmd = new SQLiteCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@DateOnlyStr", dateOnlyStr);
            cmd.Parameters.AddWithValue("@EndDateStr", endDateStr);
            cmd.Parameters.AddWithValue("@StartDateStr", startDateStr);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 聚合每日总览信息。
        /// </summary>
        // 更改方法签名，接受 DateOnlyStr
        private void AggregateDailySummary(string dateOnlyStr, string startDateStr, string endDateStr, SQLiteConnection conn, SQLiteTransaction transaction)
        {
            // 【优化点 3：UPSERT】
            // 【优化点 2：时间筛选】
            string sql = @"
                INSERT INTO DailySummary (Date, KeyCount, CopyCount, AfkSeconds, TotalActiveSeconds, WorkSeconds, GameSeconds)
                SELECT
                    @DateOnlyStr AS Date, -- 使用 @DateOnlyStr 
                    (SELECT COUNT(*) FROM KeyLogs WHERE Timestamp >= @StartDateStr AND Timestamp < @EndDateStr) AS KeyCount,
                    (SELECT COUNT(*) FROM ClipboardLogs WHERE Timestamp >= @StartDateStr AND Timestamp < @EndDateStr) AS CopyCount,
                    IFNULL((SELECT SUM(DurationSeconds) FROM AfkLogs WHERE StartTime >= @StartDateStr AND StartTime < @EndDateStr), 0) AS AfkSeconds,
                    IFNULL((SELECT SUM(DurationSeconds) FROM WindowActivities WHERE StartTime >= @StartDateStr AND StartTime < @EndDateStr), 0) AS TotalActiveSeconds,
                    
                    -- 从 CategoryTimeStats 中获取 WorkSeconds 和 GameSeconds
                    (SELECT IFNULL(ActiveSeconds, 0) FROM CategoryTimeStats WHERE Date = @DateOnlyStr AND CategoryId = 1) AS WorkSeconds, -- 假设 1 是工作分类ID
                    (SELECT IFNULL(ActiveSeconds, 0) FROM CategoryTimeStats WHERE Date = @DateOnlyStr AND CategoryId = 2) AS GameSeconds -- 假设 2 是游戏分类ID
                
                ON CONFLICT(Date) DO UPDATE SET
                    KeyCount = excluded.KeyCount,
                    CopyCount = excluded.CopyCount,
                    AfkSeconds = excluded.AfkSeconds,
                    TotalActiveSeconds = excluded.TotalActiveSeconds,
                    WorkSeconds = excluded.WorkSeconds,
                    GameSeconds = excluded.GameSeconds;
            ";

            using var cmd = new SQLiteCommand(sql, conn, transaction);
            // 🔑 修正：使用 dateOnlyStr 绑定 DailySummary 的主键 Date，并用于子查询
            cmd.Parameters.AddWithValue("@DateOnlyStr", dateOnlyStr);
            cmd.Parameters.AddWithValue("@StartDateStr", startDateStr);
            cmd.Parameters.AddWithValue("@EndDateStr", endDateStr);
            cmd.ExecuteNonQuery();
        }
    }
}