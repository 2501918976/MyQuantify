using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Repositories.Aggre
{
    public class CategoryTimeStatsRepository
    {
        private readonly string _connectionString;

        public CategoryTimeStatsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 获取指定日期范围内的分类活跃时长（包含分类名）。
        /// ⚠️ 注意：此方法用于获取饼图所需的 Activity/Category 数据。
        /// </summary>
        /// <param name="startDate">起始日期（含，格式 yyyy-MM-dd）</param>
        /// <param name="endDate">结束日期（含，格式 yyyy-MM-dd）</param>
        /// <returns>字典：键为日期字符串，值为 (CategoryName → ActiveSeconds) 的字典。</returns>
        public Dictionary<string, Dictionary<string, int>> GetCategoryTimeRangeWithNames(string startDate, string endDate)
        {
            var result = new Dictionary<string, Dictionary<string, int>>();

            string sql = @"
                SELECT Date, Name, ActiveSeconds 
                FROM CategoryTimeStats
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
                                string categoryName = reader["Name"].ToString() ?? "Unknown Category";
                                int activeSeconds = reader["ActiveSeconds"] != DBNull.Value ? Convert.ToInt32(reader["ActiveSeconds"]) : 0;

                                if (!result.ContainsKey(date))
                                    result[date] = new Dictionary<string, int>();

                                // 使用 CategoryName 作为键
                                result[date][categoryName] = activeSeconds;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "❌ 数据库查询失败：GetCategoryTimeRangeWithNames。");
                }
            }

            return result;
        }
    }
}
