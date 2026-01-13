using Microsoft.EntityFrameworkCore;
using SelfTracker.Entity.Base;
using SelfTracker.Repository;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SelfTracker.DataCollectors
{
    /// <summary>
    /// 分类匹配器：根据进程名和窗口标题匹配 Category
    /// </summary>
    public class CategoryMatcher
    {
        private readonly QuantifyDbContext _db;

        public CategoryMatcher(QuantifyDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 根据当前进程名和窗口标题匹配分类
        /// </summary>
        /// <param name="processName">进程名（如 chrome, devenv）</param>
        /// <param name="windowTitle">窗口标题</param>
        /// <returns>命中的 Category，如果没有匹配返回 null</returns>
        public Category? MatchCategory(string processName, string windowTitle)
        {
            processName = processName.ToLower();
            windowTitle = windowTitle.ToLower();

            // 1. 获取所有规则，按优先级降序
            var rules = _db.CategoryRules
                .Include(r => r.Category)
                .OrderByDescending(r => r.Priority)
                .ToList();

            // 2. 遍历规则，找到第一个命中
            foreach (var rule in rules)
            {
                switch (rule.RuleType)
                {
                    case 0: // 进程名完全匹配
                        if (processName == rule.MatchValue.ToLower())
                            return rule.Category;
                        break;

                    case 1: // 窗口名包含
                        if (windowTitle.Contains(rule.MatchValue.ToLower()))
                            return rule.Category;
                        break;

                    case 2: // 正则匹配
                        // Regex.IsMatch 会根据正则表达式判断字符串是否符合规则
                        if (Regex.IsMatch(windowTitle, rule.MatchValue, RegexOptions.IgnoreCase))
                            return rule.Category;
                        break;
                }
            }

            return null; // 没匹配到返回 null
        }
    }
}
