using MyQuantifyApp.Database.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Repositories.Raw
{
    public class CategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 插入一条新的分类记录。
        /// </summary>
        /// <param name="category">要添加的分类数据，Id 字段会被忽略。</param>
        public int AddCategory(Category category)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                INSERT INTO Categories (Name, Description) 
                VALUES (@Name, @Description);
                SELECT last_insert_rowid();"; // 返回新插入记录的 Id

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", category.Name);
                    command.Parameters.AddWithValue("@Description", category.Description ?? (object)DBNull.Value);

                    // 执行查询并返回新 Id
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// 根据 Id 删除一条分类记录。
        /// </summary>
        /// <param name="categoryId">要删除的分类的 Id。</param>
        public void DeleteCategory(int categoryId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                // 注意：由于 Categories 表被 Processes 和 Windows 表引用，
                // 数据库的外键约束 (ON DELETE SET NULL) 会自动处理关联记录。
                string sql = "DELETE FROM Categories WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", categoryId);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 更新一条已有的分类记录。
        /// </summary>
        /// <param name="category">包含 Id 和新分类数据的对象。</param>
        public void UpdateCategory(Category category)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                UPDATE Categories SET 
                Name = @Name, Description = @Description 
                WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", category.Name);
                    command.Parameters.AddWithValue("@Description", category.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Id", category.Id);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 获取数据库中所有的分类记录。
        /// </summary>
        /// <returns>包含所有 Category 对象的列表。</returns>
        public List<Category> GetAllCategories()
        {
            var categories = new List<Category>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, Name, Description FROM Categories ORDER BY Id";

                using (var command = new SQLiteCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            // Description 字段允许为 NULL
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                        });
                    }
                }
            }
            return categories;
        }

        /// <summary>
        /// 根据 Id 获取一条分类记录。
        /// </summary>
        /// <param name="categoryId">要查询的分类的 Id。</param>
        /// <returns>Category 对象，如果不存在则返回 null。</returns>
        public Category GetCategoryById(int categoryId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, Name, Description FROM Categories WHERE Id = @Id";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", categoryId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Category
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                            };
                        }
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// 获取所有分类及其关联的进程名和窗口标题。
        /// </summary>
        /// <returns>一个包含 Category 和关联进程/窗口详情的字典。</returns>
        public Dictionary<Category, List<ProcessInfo>> GetAllCategoriesWithDetails()
        {
            var result = new Dictionary<Category, List<ProcessInfo>>();
            var categoryMap = new Dictionary<int, Category>();
            var processMap = new Dictionary<int, ProcessInfo>(); // 用 ProcessId 作为唯一 Key

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string sql = @"
            SELECT 
                C.Id AS CategoryId,
                C.Name AS CategoryName,
                P.Id AS ProcessId,
                P.ProcessName AS ProcessName,
                W.WindowTitle AS WindowTitle
            FROM Categories AS C
            LEFT JOIN Processes AS P ON P.CategoryId = C.Id
            LEFT JOIN Windows AS W ON W.ProcessId = P.Id
            ORDER BY C.Name, P.ProcessName, W.WindowTitle;";

                using (var command = new SQLiteCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // 1. 读取分类 (C.Id 和 C.Name 始终非空)
                        int categoryId = reader.GetInt32(0);
                        string categoryName = reader.GetString(1);

                        // 获取或创建 Category
                        if (!categoryMap.ContainsKey(categoryId))
                        {
                            var cat = new Category { Id = categoryId, Name = categoryName };
                            categoryMap[categoryId] = cat;
                            result[cat] = new List<ProcessInfo>();
                        }
                        var currentCategory = categoryMap[categoryId];

                        // 2. 核心修正：检查 P.Id (ProcessId) 是否为 NULL
                        if (reader.IsDBNull(2)) // 索引 2 对应 P.Id
                        {
                            // 如果 P.Id 为 NULL，说明此 Category 下没有关联的进程。
                            // 由于 Category 已经处理完毕，跳过本次循环，避免读取进程字段时抛出异常。
                            continue;
                        }

                        // 3. 读取进程（P.Id 此时保证非 NULL）
                        int processId = reader.GetInt32(2); // 此时安全地读取 Int32
                        string processName = reader.GetString(3);

                        // 4. 读取窗口（W.WindowTitle 可能为 NULL）
                        // 使用三元运算符检查 IsDBNull，如果为 NULL，则设为 null，否则读取字符串。
                        string windowTitle = reader.IsDBNull(4) ? null : reader.GetString(4);

                        // 获取或创建 ProcessInfo
                        if (!processMap.ContainsKey(processId))
                        {
                            var proc = new ProcessInfo
                            {
                                Id = processId,
                                ProcessName = processName,
                                Name = processName,
                                CategoryId = categoryId,
                                Windows = new List<string>()
                            };
                            processMap[processId] = proc;
                            result[currentCategory].Add(proc);
                        }

                        // 添加窗口标题
                        // 检查 windowTitle 是否为 null 或空字符串
                        if (!string.IsNullOrEmpty(windowTitle) && !processMap[processId].Windows.Contains(windowTitle))
                        {
                            processMap[processId].Windows.Add(windowTitle);
                        }
                    }

                return result;
        }

            }

        }
    }
}