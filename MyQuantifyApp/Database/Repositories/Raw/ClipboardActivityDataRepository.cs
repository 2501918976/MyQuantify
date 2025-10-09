using MyQuantifyApp.Database.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Repositories.Raw
{
    public class ClipboardActivityDataRepository
    {
        private readonly string _connectionString;

        public ClipboardActivityDataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 插入一条剪贴板记录
        public void AddClipboardLog(ClipboardActivityData log)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                    INSERT INTO ClipboardLogs (Content, Length, Timestamp) 
                    VALUES (@Content, @Length, @Timestamp);";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Content", log.Content ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Length", log.Length);
                    command.Parameters.AddWithValue("@Timestamp", log.Timestamp);
                    command.ExecuteNonQuery();
                }
            }
        }

        // 获取指定日期范围内的剪贴记录（仅部分内容）
        public List<ClipboardActivityData> GetClipboardLogsInRange(DateTime start, DateTime end, int maxLength = 100)
        {
            var result = new List<ClipboardActivityData>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = $@"
                    SELECT Id, 
                           substr(Content, 1, @MaxLength) AS Content, 
                           Length, 
                           Timestamp
                    FROM ClipboardLogs
                    WHERE Timestamp BETWEEN @Start AND @End
                    ORDER BY Timestamp DESC;";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Start", start);
                    command.Parameters.AddWithValue("@End", end);
                    command.Parameters.AddWithValue("@MaxLength", maxLength);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new ClipboardActivityData
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Content = reader["Content"].ToString() ?? "",
                                Length = Convert.ToInt32(reader["Length"]),
                                Timestamp = Convert.ToDateTime(reader["Timestamp"])
                            });
                        }
                    }
                }
            }

            return result;
        }

        // 获取指定 ID 的完整剪贴内容
        public string? GetFullClipboardContentById(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Content FROM ClipboardLogs WHERE Id = @Id;";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    var result = command.ExecuteScalar();
                    return result == DBNull.Value ? null : result?.ToString();
                }
            }
        }
    }
}