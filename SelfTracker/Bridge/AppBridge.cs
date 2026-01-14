using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SelfTracker.Bridge
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class AppBridge
    {
        private readonly MainWindow _mainWindow;

        public AppBridge(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        // ==================== 窗口控制 ====================

        public void minimize()
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.WindowState = System.Windows.WindowState.Minimized;
            });
        }

        public void close()
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.Close();
            });
        }

        // ==================== 今日统计 ====================

        public async Task<string> GetTodayStats()
        {
            // TODO: 从数据库获取今日统计数据
            var stats = new
            {
                score = 85,
                totalKeystrokes = 8542,
                totalCopies = 156,
                activeTime = 4.2,
                afkTime = 0.8,
                growthRate = 12,
                currentStatus = "深度工作"
            };

            return JsonSerializer.Serialize(stats);
        }

        public async Task<string> Get24HActivityMap()
        {
            // TODO: 从数据库获取 24 小时活动数据
            var activityMap = new
            {
                application = new int[24] { 0, 0, 2, 5, 8, 9, 7, 6, 8, 9, 8, 7, 6, 7, 8, 9, 8, 7, 5, 4, 3, 2, 1, 0 },
                typing = new int[24] { 0, 0, 1, 3, 7, 8, 6, 5, 7, 8, 7, 6, 5, 6, 7, 8, 7, 6, 4, 3, 2, 1, 0, 0 },
                copying = new int[24] { 0, 0, 0, 2, 4, 5, 4, 3, 4, 5, 4, 3, 2, 3, 4, 5, 4, 3, 2, 1, 1, 0, 0, 0 }
            };

            return JsonSerializer.Serialize(activityMap);
        }

        public async Task<string> GetCategoryRanking()
        {
            // TODO: 从数据库获取分类排行
            var ranking = new[]
            {
                new { categoryName = "办公开发", icon = "💼", duration = 2.5, percentage = 62, color = "#4e73df" },
                new { categoryName = "娱乐休闲", icon = "🎮", duration = 1.2, percentage = 30, color = "#1cc88a" },
                new { categoryName = "学习阅读", icon = "📚", duration = 0.8, percentage = 20, color = "#36b9cc" }
            };

            return JsonSerializer.Serialize(ranking);
        }

        public async Task<string> GetCurrentProcess()
        {
            // TODO: 获取当前活动进程
            var process = new
            {
                processName = "Code.exe",
                windowTitle = "VS Code - App.js",
                icon = "💻",
                duration = 45,
                status = "运行中"
            };

            return JsonSerializer.Serialize(process);
        }

        // ==================== 历史数据 ====================

        public async Task<string> GetHistoryStats(string paramsJson)
        {
            var parameters = JsonSerializer.Deserialize<HistoryQueryParams>(paramsJson);

            // TODO: 根据日期范围从数据库查询历史数据
            var historyStats = new
            {
                dates = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                values = new[] { 4200, 5800, 7200, 6100, 8900, 3200, 2100 },
                totalKeystrokes = 37500,
                totalCopies = 452,
                totalHours = 42.5
            };

            return JsonSerializer.Serialize(historyStats);
        }

        // ==================== 规则引擎 ====================

        public async Task<string> 获取所有分类()
        {
            // TODO: 从数据库获取所有分类
            var categories = new[]
            {
                new
                {
                    Id = 1,
                    CategoryName = "办公开发",
                    ColorCode = "#4e73df",
                    CategoryRules = new object[] { }
                },
                new
                {
                    Id = 2,
                    CategoryName = "娱乐休闲",
                    ColorCode = "#1cc88a",
                    CategoryRules = new object[] { }
                }
            };

            return JsonSerializer.Serialize(categories);
        }

        public async Task<string> 获取未分类的活动()
        {
            // TODO: 获取未分类的活动记录
            var unclassified = new[]
            {
                new { ProcessName = "chrome.exe", WindowTitle = "Google Chrome - Stack Overflow" },
                new { ProcessName = "notepad.exe", WindowTitle = "无标题 - 记事本" }
            };

            return JsonSerializer.Serialize(unclassified);
        }

        public async Task 新增一个规则(string ruleDataJson)
        {
            var ruleData = JsonSerializer.Deserialize<RuleData>(ruleDataJson);
            // TODO: 保存规则到数据库
        }

        public async Task 修改一个规则(string ruleDataJson)
        {
            var ruleData = JsonSerializer.Deserialize<RuleData>(ruleDataJson);
            // TODO: 更新规则到数据库
        }

        public async Task 删除一个规则(string ruleDataJson)
        {
            var data = JsonSerializer.Deserialize<IdData>(ruleDataJson);
            // TODO: 从数据库删除规则
        }

        public async Task 新增一个分类(string categoryDataJson)
        {
            var categoryData = JsonSerializer.Deserialize<CategoryData>(categoryDataJson);
            // TODO: 保存分类到数据库
        }

        public async Task 新增修改分类(string categoryDataJson)
        {
            var categoryData = JsonSerializer.Deserialize<CategoryData>(categoryDataJson);
            // TODO: 更新分类到数据库
        }

        // ==================== 系统设置 ====================

        public async Task<string> GetSystemSettings()
        {
            // TODO: 获取系统设置
            var settings = new
            {
                writeInterval = 300,
                afkTime = 120,
                filterTime = 3,
                autoStart = false,
                minimizeToTray = true,
                showNotifications = true
            };

            return JsonSerializer.Serialize(settings);
        }

        public async Task SaveSystemSettings(string settingsDataJson)
        {
            var settingsData = JsonSerializer.Deserialize<SystemSettings>(settingsDataJson);
            // TODO: 保存系统设置
        }

        public async Task MergeDatabase()
        {
            // TODO: 合并优化数据库
        }

        public async Task ExportData(string exportOptionsJson)
        {
            var options = JsonSerializer.Deserialize<ExportOptions>(exportOptionsJson);
            // TODO: 导出数据
        }

        public async Task ClearAllData()
        {
            // TODO: 清空所有数据
        }

        public async Task<string> GetDatabaseInfo()
        {
            // TODO: 获取数据库信息
            var dbInfo = new
            {
                size = 128.5,
                recordCount = 45672
            };

            return JsonSerializer.Serialize(dbInfo);
        }
    }
}
