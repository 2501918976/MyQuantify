using MyQuantifyApp.Database.Models;
using MyQuantifyApp.Services;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;

namespace MyQuantifyApp.Database.Repositories.Raw
{
    public class WindowActivityRepository
    {
        private readonly string _connectionString;
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        public WindowActivityRepository(string connectionString)
        {
            _connectionString = connectionString;
        }


        /// <summary>
        /// 插入一条新的窗口活动记录（通常只包含 StartTime）。
        /// </summary>
        /// <param name="activity">要添加的活动数据。</param>
        /// <returns>新插入记录的 Id。</returns>
        public int AddWindowActivity(Models.WindowActivity activity)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                INSERT INTO WindowActivities (WindowId, StartTime, EndTime, DurationSeconds)
                VALUES (@WindowId, @Start, @End, @Duration);
                SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@WindowId", activity.WindowId);

                    // StartTime 必须有值
                    command.Parameters.AddWithValue("@Start", activity.StartTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture));

                    // EndTime 和 DurationSeconds 允许为 NULL
                    command.Parameters.AddWithValue("@End", activity.EndTime.HasValue ? activity.EndTime.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture) : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Duration", activity.DurationSeconds.HasValue ? (object)activity.DurationSeconds.Value : DBNull.Value);

                    // 执行查询并返回新 Id
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }


        /// <summary>
        /// 更新一条窗口活动记录（通常用于设置 EndTime 和 DurationSeconds）。
        /// </summary>
        /// <param name="activity">包含 Id、EndTime 和 DurationSeconds 的活动数据。</param>
        public void UpdateWindowActivity(Models.WindowActivity activity)
        {
            if (!(activity.Id > 0))
            {
                // Log or throw an exception if Id is not set, as update is impossible
                // Log.Warning("尝试更新 WindowActivity 失败：Id 未设置。");
                return;
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                UPDATE WindowActivities SET 
                EndTime = @End, DurationSeconds = @Duration
                WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    // EndTime 和 DurationSeconds 必须有值才能更新
                    command.Parameters.AddWithValue("@End", activity.EndTime.HasValue ? activity.EndTime.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture) : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Duration", activity.DurationSeconds.HasValue ? (object)activity.DurationSeconds.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@Id", activity.Id);

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 批量插入窗口活动记录，使用事务提高性能。
        /// </summary>
        public void AddBatchWindowActivities(IEnumerable<Models.WindowActivity> activities)
        {
            if (activities == null || !activities.Any())
                return;

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 【性能优化】使用事务包裹批量插入
                using (var transaction = connection.BeginTransaction())
                {
                    string sql = @"
                    INSERT INTO WindowActivities (WindowId, StartTime, EndTime, DurationSeconds)
                    VALUES (@WindowId, @Start, @End, @Duration)";

                    using (var command = new SQLiteCommand(sql, connection, transaction))
                    {
                        var windowIdParam = command.Parameters.Add("@WindowId", System.Data.DbType.Int32);
                        var startParam = command.Parameters.Add("@Start", System.Data.DbType.String);
                        var endParam = command.Parameters.Add("@End", System.Data.DbType.String);
                        var durationParam = command.Parameters.Add("@Duration", System.Data.DbType.Int32);

                        foreach (var activity in activities)
                        {
                            windowIdParam.Value = activity.WindowId;
                            startParam.Value = activity.StartTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

                            // 处理可空字段
                            endParam.Value = activity.EndTime.HasValue
                                ? activity.EndTime.Value.ToString(DateTimeFormat, CultureInfo.InvariantCulture)
                                : (object)DBNull.Value;

                            durationParam.Value = activity.DurationSeconds.HasValue
                                ? (object)activity.DurationSeconds.Value
                                : DBNull.Value;

                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// 根据 Id 获取一条活动记录。
        /// </summary>
        public Models.WindowActivity? GetActivityById(int activityId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, WindowId, StartTime, EndTime, DurationSeconds FROM WindowActivities WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", activityId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapRowToWindowActivityData(reader);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 将 SQLiteDataReader 的当前行映射到 WindowActivityData 模型对象。
        /// </summary>
        private Models.WindowActivity MapRowToWindowActivityData(SQLiteDataReader reader)
        {
            var activity = new Models.WindowActivity
            {
                Id = reader.GetInt32(0),
                WindowId = reader.GetInt32(1),
                // StartTime 不能为空
                StartTime = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
            };

            // EndTime 允许为 NULL
            if (!reader.IsDBNull(3))
            {
                activity.EndTime = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture);
            }

            // DurationSeconds 允许为 NULL
            if (!reader.IsDBNull(4))
            {
                activity.DurationSeconds = reader.GetInt32(4);
            }

            return activity;
        }
    }
}