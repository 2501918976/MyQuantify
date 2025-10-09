using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using MyQuantifyApp.Database.Models.Aggre;

namespace MyQuantifyApp.Database.Repositories.Aggre
{
    public class DailySummaryRepository
    {
        private readonly string _connectionString;

        public DailySummaryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 辅助方法：安全地将数据库对象转换为 int，如果为 DBNull 则返回 0
        // 这是解决 InvalidCastException 的关键
        private static int GetInt32Safe(SQLiteDataReader reader, string columnName)
        {
            // 检查对应列的值是否是 DBNull
            if (reader[columnName] == DBNull.Value)
            {
                return 0;
            }
            // 如果不是 DBNull，则安全地转换为 int
            return Convert.ToInt32(reader[columnName]);
        }


        // ────────────────────────────────
        // 查询今日数据
        // ────────────────────────────────
        public DailySummary? GetTodaySummary()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            return GetSummaryByDate(today);
        }

        // ────────────────────────────────
        // 查询指定日期数据
        // ────────────────────────────────
        public DailySummary? GetSummaryByDate(string date)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"SELECT * FROM DailySummary WHERE Date = @Date";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Date", date);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new DailySummary
                            {
                                Date = reader["Date"].ToString(),
                                // 使用 GetInt32Safe 辅助方法处理可能为 NULL 的整数列
                                KeyCount = GetInt32Safe(reader, "KeyCount"),
                                CopyCount = GetInt32Safe(reader, "CopyCount"),
                                AfkSeconds = GetInt32Safe(reader, "AfkSeconds"),
                                WorkSeconds = GetInt32Safe(reader, "WorkSeconds"),
                                GameSeconds = GetInt32Safe(reader, "GameSeconds"),
                                TotalActiveSeconds = GetInt32Safe(reader, "TotalActiveSeconds")
                            };
                        }
                    }
                }
            }
            return null;
        }

        // ────────────────────────────────
        // 查询最近 7 天数据
        // ────────────────────────────────
        public List<DailySummary> GetLast7Days()
        {
            DateTime start = DateTime.Now.AddDays(-6); // 包含今天
            return GetSummariesInRange(start, DateTime.Now);
        }

        // ────────────────────────────────
        // 查询最近 30 天数据
        // ────────────────────────────────
        public List<DailySummary> GetLast30Days()
        {
            DateTime start = DateTime.Now.AddDays(-29);
            return GetSummariesInRange(start, DateTime.Now);
        }

        // ────────────────────────────────
        // 通用查询：日期范围
        // ────────────────────────────────
        private List<DailySummary> GetSummariesInRange(DateTime start, DateTime end)
        {
            var list = new List<DailySummary>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                SELECT * FROM DailySummary 
                WHERE Date BETWEEN @StartDate AND @EndDate
                ORDER BY Date ASC";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", start.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@EndDate", end.ToString("yyyy-MM-dd"));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DailySummary
                            {
                                Date = reader["Date"].ToString(),
                                // 使用 GetInt32Safe 辅助方法处理可能为 NULL 的整数列
                                KeyCount = GetInt32Safe(reader, "KeyCount"),
                                CopyCount = GetInt32Safe(reader, "CopyCount"),
                                AfkSeconds = GetInt32Safe(reader, "AfkSeconds"),
                                WorkSeconds = GetInt32Safe(reader, "WorkSeconds"),
                                GameSeconds = GetInt32Safe(reader, "GameSeconds"),
                                TotalActiveSeconds = GetInt32Safe(reader, "TotalActiveSeconds")
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}