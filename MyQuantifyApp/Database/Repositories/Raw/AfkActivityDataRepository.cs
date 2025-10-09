using MyQuantifyApp.Database.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Repositories.Raw
{
    public class AfkActivityDataRepository
    {
        private readonly string _connectionString;

        public AfkActivityDataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 插入一条新的离线（Afk）活动记录。
        /// </summary>
        public void AddAfkLog(AfkData log)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                INSERT INTO AfkLogs (StartTime, EndTime, DurationSeconds) 
                VALUES (@Start, @End, @Duration)";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Start", log.StartTime);
                    command.Parameters.AddWithValue("@End", log.EndTime);
                    command.Parameters.AddWithValue("@Duration", log.DurationSeconds);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 更新离线活动记录（通常用于修正 EndTime/Duration）。
        /// </summary>
        public void UpdateAfkLog(AfkData log)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                UPDATE AfkLogs SET 
                StartTime = @Start, EndTime = @End, DurationSeconds = @Duration 
                WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Start", log.StartTime);
                    command.Parameters.AddWithValue("@End", log.EndTime);
                    command.Parameters.AddWithValue("@Duration", log.DurationSeconds);
                    command.Parameters.AddWithValue("@Id", log.Id);
                    command.ExecuteNonQuery();
                }
            }
        }


    }
}