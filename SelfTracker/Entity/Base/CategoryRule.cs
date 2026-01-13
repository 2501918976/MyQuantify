using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity.Base
{
    public class CategoryRule
    {
        [Key]
        public int Id { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; } = null!;

        /// <summary>
        /// 0: 进程名等于
        /// 1: 窗口名包含
        /// 2: 正则匹配
        /// </summary>
        public int RuleType { get; set; }

        [Required]
        public string MatchValue { get; set; } = string.Empty;

        /// <summary>
        /// 数值越大优先级越高
        /// </summary>
        public int Priority { get; set; }
    }

}
