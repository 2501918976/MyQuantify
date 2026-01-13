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
    /// 进程表：应用元数据
    /// </summary>
    public class ProcessInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// 当前规则计算下的分类结果（可重算）
        /// </summary>
        public int? CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    }

}
