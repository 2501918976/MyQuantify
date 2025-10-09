using MyQuantifyApp.Database.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Repositories.Raw
{
    public class KeyCharDataRepository
    {
        private readonly string _connectionString;

        public KeyCharDataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 批量插入多条按键记录。
        /// </summary>
        public void AddKeyLogs(List<KeyCharData> logs)
        {
            if (logs == null || logs.Count == 0) return;

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 使用事务和参数化查询进行高性能批量插入
                using (var transaction = connection.BeginTransaction())
                {
                    string sql = "INSERT INTO KeyLogs (KeyChar, Timestamp) VALUES (@KeyChar, @Timestamp)";
                    using (var command = new SQLiteCommand(sql, connection, transaction))
                    {
                        // 预先添加参数，以便在循环中重用
                        command.Parameters.Add("@KeyChar", System.Data.DbType.String);
                        command.Parameters.Add("@Timestamp", System.Data.DbType.String);

                        foreach (var log in logs)
                        {
                            command.Parameters["@KeyChar"].Value = log.KeyChar;
                            // 使用 ISO 8601 格式确保 SQLite 正确解析日期时间
                            command.Parameters["@Timestamp"].Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// 插入一条新的按键记录。
        /// </summary>
        public void AddKeyLog(KeyCharData log)
        {
            // 保持原有的单条插入方法，但推荐使用 AddKeyLogs
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "INSERT INTO KeyLogs (KeyChar, Timestamp) VALUES (@KeyChar, @Timestamp)";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@KeyChar", log.KeyChar);
                    command.Parameters.AddWithValue("@Timestamp", log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}