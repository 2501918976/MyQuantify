using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity.Base
{

    public class ScoreRule
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 规则名称（用于显示 / 管理）
        /// </summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 规则类型（决定计算方式）
        /// </summary>
        [Required]
        public ScoreRuleType RuleType { get; set; }

        /// <summary>
        /// 权重（对总分的影响）
        /// </summary>
        public int Weight { get; set; } = 1;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public enum ScoreRuleType
    {
        ActivityDuration,   // 使用时长
        FocusRatio,         // 专注度
        CategoryBalance,    // 分类均衡
        IdlePenalty,        // 空闲惩罚
        Custom              // 预留
    }

}
