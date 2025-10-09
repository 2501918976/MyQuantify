using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Repositories.Aggre
{
    // 假设 ProcessTimeStats 表包含 Date, ProcessName, ActiveSeconds 字段
    public class ProcessTimeStatsRepository
    {
        private readonly string _connectionString;

        public ProcessTimeStatsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 获取指定日期范围内的进程活跃时长（包含进程名）。
        /// ⚠️ 注意：此方法用于获取饼图所需的 App/进程数据 (即前端 Type="App")。
        /// </summary>
        /// <param name="startDate">起始日期（含，格式 yyyy-MM-dd）</param>
        /// <param name="endDate">结束日期（含，格式 yyyy-MM-dd）</param>
        /// <returns>字典：键为日期字符串，值为 (ProcessName → ActiveSeconds) 的字典。</returns>
        public Dictionary<string, Dictionary<string, int>> GetProcessTimeRangeWithNames(string startDate, string endDate)
        {
            var result = new Dictionary<string, Dictionary<string, int>>();

            string sql = @"
                SELECT Date, ProcessName, ActiveSeconds 
                FROM ProcessTimeStats
                WHERE Date BETWEEN @StartDate AND @EndDate
                ORDER BY Date ASC, ActiveSeconds DESC;";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                try
                {
                    connection.Open();

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string date = reader["Date"].ToString() ?? "";
                                string processName = reader["ProcessName"].ToString() ?? "Unknown Process";
                                int activeSeconds = reader["ActiveSeconds"] != DBNull.Value ? Convert.ToInt32(reader["ActiveSeconds"]) : 0;

                                if (!result.ContainsKey(date))
                                    result[date] = new Dictionary<string, int>();

                                // 使用 ProcessName 作为键
                                result[date][processName] = activeSeconds;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return result;
        }
    }
}
