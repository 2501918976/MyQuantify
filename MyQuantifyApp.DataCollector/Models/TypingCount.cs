using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.DataCollector.Models
{
    /// <summary>
    /// 用于记录在特定时间窗口内的按键总数。
    /// </summary>
    public class TypingCount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        // 记录的时间点，即聚合周期结束的时间
        public DateTime Timestamp { get; set; }

        [Required]
        // 记录的按键总数（过滤掉修饰键）
        public int KeyPressCount { get; set; }

        public int? ActivitySessionId { get; set; }

        [ForeignKey("ActivitySessionId")]
        public virtual ActivitySession? ActivitySession { get; set; }

    }
}
