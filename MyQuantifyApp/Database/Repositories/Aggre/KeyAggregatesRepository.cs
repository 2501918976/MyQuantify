using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Repositories.Aggre
{
    public class KeyAggregatesRepository
    {
        private readonly string _connectionString;

        public KeyAggregatesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 获取指定日期的每个按键的按下次数。
        /// </summary>
        /// <param name="date">要查询的日期字符串（格式为 yyyy-MM-dd）。</param>
        /// <returns>一个字典，键为 KeyChar，值为 Count。</returns>
        public Dictionary<string, int> GetKeyCountsByDate(string date)
        {
            var keyCounts = new Dictionary<string, int>();

            // SQL 语句：查询指定日期的 KeyChar 和 Count
            string sql = @"
                SELECT KeyChar, Count 
                FROM KeyAggregates 
                WHERE Date = @Date 
                ORDER BY Count DESC;";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Date", date);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // 确保 KeyChar 和 Count 字段存在且非空
                            string keyChar = reader["KeyChar"].ToString();

                            // 使用安全转换，避免 DBNull 异常，但根据您的表结构，Count 默认为 0 应该不会为 NULL
                            int count = 0;
                            if (reader["Count"] != DBNull.Value)
                            {
                                count = Convert.ToInt32(reader["Count"]);
                            }

                            // 将结果添加到字典中
                            if (!string.IsNullOrEmpty(keyChar))
                            {
                                // 注意：KeyChar 可能存储为 ShiftLeft, CtrlRight 等，这正是 JS 页面需要的键名
                                keyCounts[keyChar] = count;
                            }
                        }
                    }
                }
            }

            return keyCounts;
        }

        // 示例：您可能需要的另一个获取今日数据的方法
        public Dictionary<string, int> GetTodayKeyCounts()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            return GetKeyCountsByDate(today);
        }
    }
}
