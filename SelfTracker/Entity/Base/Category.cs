using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity.Base
{
    /// <summary>
    /// 分类表：定义应用所属的类别
    /// </summary>
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? ColorCode { get; set; }

        public DateTime LastModifiedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 分类规则（权威来源）
        /// </summary>
        public virtual ICollection<CategoryRule> Rules { get; set; } = new List<CategoryRule>();

        /// <summary>
        /// 当前规则命中的进程（视图/缓存）
        /// </summary>
        public virtual ICollection<ProcessInfo> Processes { get; set; } = new List<ProcessInfo>();
    }

}
