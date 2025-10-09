using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyQuantifyApp.Database.Models;

namespace MyQuantifyApp.Database.Repositories.Raw
{
    public class WindowRepository
    {
        private readonly string _connectionString;

        public WindowRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 插入一条新的窗口记录。
        /// </summary>
        /// <param name="window">要添加的窗口数据。</param>
        /// <returns>新插入记录的 Id。</returns>
        public int AddWindow(WindowInfo window)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                INSERT INTO Windows (ProcessId, WindowTitle, CategoryId)
                VALUES (@ProcessId, @Title, @CategoryId);
                SELECT last_insert_rowid();"; // 确保返回新 Id

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ProcessId", window.ProcessId);
                    command.Parameters.AddWithValue("@Title", window.WindowTitle);
                    // CategoryId 允许为 NULL
                    command.Parameters.AddWithValue("@CategoryId", window.CategoryId.HasValue ? (object)window.CategoryId.Value : DBNull.Value);

                    // 执行查询并返回新 Id
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// 根据 Id 删除一条窗口记录。
        /// </summary>
        /// <param name="windowId">要删除的窗口的 Id。</param>
        public void DeleteWindow(int windowId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                // Windows 表被 WindowActivities 引用 (ON DELETE CASCADE)，
                // 删除窗口将级联删除所有相关的活动记录。
                string sql = "DELETE FROM Windows WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", windowId);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 更新窗口的分类 ID。
        /// </summary>
        public void UpdateWindowCategory(int windowId, int? categoryId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "UPDATE Windows SET CategoryId = @CategoryId WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CategoryId", categoryId.HasValue ? (object)categoryId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@Id", windowId);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 获取数据库中所有的窗口记录。
        /// </summary>
        /// <returns>包含所有 WindowInfo 对象的列表。</returns>
        public List<WindowInfo> GetAllWindows()
        {
            var windows = new List<WindowInfo>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, ProcessId, WindowTitle, CategoryId FROM Windows ORDER BY WindowTitle";

                using (var command = new SQLiteCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        windows.Add(MapRowToWindowInfo(reader));
                    }
                }
            }
            return windows;
        }

        /// <summary>
        /// 根据进程 ID 和窗口标题查找窗口记录，如果不存在则创建并返回新记录。
        /// 这是最常用的方法，用于处理窗口切换事件。
        /// </summary>
        /// <param name="processId">关联的进程 ID。</param>
        /// <param name="windowTitle">窗口的完整标题。</param>
        /// <param name="categoryId">如果创建新窗口，使用的初始分类 ID (通常来自进程)。</param>
        /// <returns>找到或创建的 WindowInfo 对象。</returns>
        public WindowInfo FindOrCreateWindow(int processId, string windowTitle, int? categoryId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 1. 尝试查找 (ProcessId 和 WindowTitle 构成 UNIQUE 约束)
                string selectSql = "SELECT Id, ProcessId, WindowTitle, CategoryId FROM Windows WHERE ProcessId = @ProcessId AND WindowTitle = @Title";

                using (var selectCommand = new SQLiteCommand(selectSql, connection))
                {
                    selectCommand.Parameters.AddWithValue("@ProcessId", processId);
                    selectCommand.Parameters.AddWithValue("@Title", windowTitle);

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // 找到记录，直接返回
                            return MapRowToWindowInfo(reader);
                        }
                    }
                }

                // 2. 未找到，创建新记录
                var newWindow = new WindowInfo
                {
                    ProcessId = processId,
                    WindowTitle = windowTitle,
                    CategoryId = categoryId // 使用传入的初始分类 ID
                };

                // 确保创建操作在事务中进行（如果需要，但此处简化为单个命令）
                newWindow.Id = AddWindow(newWindow);
                return newWindow;
            }
        }

        /// <summary>
        /// 将 SQLiteDataReader 的当前行映射到 WindowInfo 模型对象。
        /// </summary>
        private WindowInfo MapRowToWindowInfo(SQLiteDataReader reader)
        {
            return new WindowInfo
            {
                Id = reader.GetInt32(0),
                ProcessId = reader.GetInt32(1),
                WindowTitle = reader.GetString(2),
                // CategoryId 允许为 NULL
                CategoryId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3)
            };
        }
    }
}
