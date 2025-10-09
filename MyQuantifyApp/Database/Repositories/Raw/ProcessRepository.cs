using MyQuantifyApp.Database.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;

namespace MyQuantifyApp.Database.Repositories.Raw
{
    public class ProcessRepository
    {
        private readonly string _connectionString;

        public ProcessRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 插入一条新的进程记录。
        /// </summary>
        /// <param name="process">要添加的进程数据，Id 字段会被忽略。</param>
        /// <returns>新插入记录的 Id。</returns>
        public int AddProcess(ProcessInfo process)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                INSERT INTO Processes (ProcessName, FilePath, CategoryId) 
                VALUES (@ProcessName, @FilePath, @CategoryId);
                SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ProcessName", process.ProcessName);
                    command.Parameters.AddWithValue("@FilePath", process.FilePath ?? (object)DBNull.Value);

                    // CategoryId 允许为 NULL，需要正确处理
                    command.Parameters.AddWithValue("@CategoryId", process.CategoryId.HasValue ? (object)process.CategoryId.Value : DBNull.Value);

                    // 执行查询并返回新 Id
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// 根据 Id 删除一条进程记录。
        /// </summary>
        /// <param name="processId">要删除的进程的 Id。</param>
        public void DeleteProcess(int processId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                // 注意：Processes 表被 Windows 表引用 (ON DELETE CASCADE)，
                // 删除进程将级联删除所有相关的窗口和窗口活动记录。
                string sql = "DELETE FROM Processes WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", processId);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 根据 Id 获取一条进程记录。
        /// </summary>
        /// <param name="processId">要查询的进程的 Id。</param>
        /// <returns>Process 对象，如果不存在则返回 null。</returns>
        public ProcessInfo GetProcessById(int processId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, ProcessName, FilePath, CategoryId FROM Processes WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", processId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapRowToProcess(reader);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取数据库中所有的进程记录。
        /// </summary>
        /// <returns>包含所有 Process 对象的列表。</returns>
        public List<ProcessInfo> GetAllProcesses()
        {
            var processes = new List<ProcessInfo>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, ProcessName, FilePath, CategoryId FROM Processes ORDER BY ProcessName";

                using (var command = new SQLiteCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        processes.Add(MapRowToProcess(reader));
                    }
                }
            }
            return processes;
        }

        /// <summary>
        /// 根据分类 Id 获取所有关联的进程记录。
        /// </summary>
        /// <param name="categoryId">分类的 Id。</param>
        /// <returns>包含指定分类 Process 对象的列表。</returns>
        public List<ProcessInfo> GetProcessesByCategoryId(int categoryId)
        {
            var processes = new List<ProcessInfo>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, ProcessName, FilePath, CategoryId FROM Processes WHERE CategoryId = @CategoryId ORDER BY ProcessName";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CategoryId", categoryId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            processes.Add(MapRowToProcess(reader));
                        }
                    }
                }
            }
            return processes;
        }

        /// <summary>
        /// 获取所有未设置分类（CategoryId 为 NULL）的进程记录。
        /// </summary>
        /// <returns>包含未分类 Process 对象的列表。</returns>
        public List<ProcessInfo> GetUncategorizedProcesses()
        {
            var processes = new List<ProcessInfo>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, ProcessName, FilePath, CategoryId FROM Processes WHERE CategoryId IS NULL ORDER BY ProcessName";

                using (var command = new SQLiteCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        processes.Add(MapRowToProcess(reader));
                    }
                }
            }
            return processes;
        }

        /// <summary>
        /// 将 SQLiteDataReader 的当前行映射到 Process 模型对象。
        /// </summary>
        private ProcessInfo MapRowToProcess(SQLiteDataReader reader)
        {
            return new ProcessInfo
            {
                Id = reader.GetInt32(0),
                ProcessName = reader.GetString(1),
                // FilePath 允许为 NULL
                FilePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                // CategoryId 允许为 NULL
                CategoryId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3)
            };
        }

        /// <summary>
        /// 根据进程名称和文件路径查找进程记录，如果不存在则创建并返回新记录。
        /// </summary>
        /// <param name="processName">进程名称。</param>
        /// <param name="filePath">文件路径（可为 null）。</param>
        /// <returns>找到或创建的 ProcessInfo 对象。</returns>
        public ProcessInfo FindOrCreateProcess(string processName, string? filePath)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 1. 尝试查找
                string selectSql = "SELECT Id, ProcessName, FilePath, CategoryId FROM Processes WHERE ProcessName = @ProcessName AND (FilePath = @FilePath OR (FilePath IS NULL AND @FilePath IS NULL))";

                using (var selectCommand = new SQLiteCommand(selectSql, connection))
                {
                    selectCommand.Parameters.AddWithValue("@ProcessName", processName);
                    selectCommand.Parameters.AddWithValue("@FilePath", filePath ?? (object)DBNull.Value);

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // 找到记录，直接返回
                            return MapRowToProcessInfo(reader);
                        }
                    }
                }

                var newProcess = new ProcessInfo
                {
                    ProcessName = processName,
                    FilePath = filePath,
                    // ⭐️ 新记录默认 CategoryId 为 1 (假设 1 是“未分类”)
                    CategoryId = 1
                };

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertSql = @"
                INSERT INTO Processes (ProcessName, FilePath, CategoryId) 
                VALUES (@ProcessName, @FilePath, @CategoryId);
                SELECT last_insert_rowid();";

                        using (var insertCommand = new SQLiteCommand(insertSql, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@ProcessName", newProcess.ProcessName);
                            insertCommand.Parameters.AddWithValue("@FilePath", newProcess.FilePath ?? (object)DBNull.Value);

                            // ⭐️ 核心修改：将 CategoryId 默认值设置为 1，而不是 DBNull.Value
                            insertCommand.Parameters.AddWithValue("@CategoryId", newProcess.CategoryId.Value); // 使用 newProcess.CategoryId 的值 (即 1)

                            newProcess.Id = Convert.ToInt32(insertCommand.ExecuteScalar());
                        }
                        transaction.Commit();
                        return newProcess;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        // 记录日志或处理错误...
                        throw new Exception($"查找或创建进程失败: {ex.Message}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// 根据进程名称更新其分类 Id (CategoryId)。
        /// </summary>
        /// <param name="processName">要更新的进程名称 (ProcessName)。</param>
        /// <param name="newCategoryId">新的分类 Id。传入 null 则表示取消分类 (设为 NULL)。</param>
        /// <returns>受影响的行数。</returns>
        public int UpdateProcessCategory(string processName, int? newCategoryId)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                // Log.Warning("尝试更新进程分类时，进程名称为空。");
                return 0;
            }
            Log.Debug("更新进程 {ProcessName} 的 CategoryId 为 {NewId}", processName, newCategoryId);

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 更新 ProcessName 匹配的所有记录的 CategoryId
                string sql = @"
                    UPDATE Processes 
                    SET CategoryId = @NewCategoryId
                    WHERE ProcessName = @ProcessName";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ProcessName", processName);

                    // CategoryId 允许为 NULL，需要正确处理
                    command.Parameters.AddWithValue("@NewCategoryId", newCategoryId.HasValue ? (object)newCategoryId.Value : DBNull.Value);

                    int rowsAffected = command.ExecuteNonQuery();
                    // Debug.WriteLine($"[DB LOG] 更新进程 '{processName}' 的分类到 ID {newCategoryId}，影响 {rowsAffected} 行。");
                    return rowsAffected;
                }
            }
        }

        /// <summary>
        /// 将 SQLiteDataReader 的当前行映射到 ProcessInfo 模型对象。
        /// </summary>
        private ProcessInfo MapRowToProcessInfo(SQLiteDataReader reader)
        {
            var info = new ProcessInfo
            {
                Id = reader.GetInt32(0),
                ProcessName = reader.GetString(1),
                // FilePath 允许为 NULL
                FilePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                // CategoryId 允许为 NULL
                CategoryId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3)
            };

            // 默认将 Name 字段设置为 ProcessName，方便前端展示
            info.Name = info.ProcessName;

            return info;
        }
    }
}