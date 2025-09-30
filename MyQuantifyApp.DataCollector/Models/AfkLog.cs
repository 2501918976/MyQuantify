using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.DataCollector.Models
{
    public class AfkLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime AfkStartTime { get; set; }

        // 您指定的最后一次活动时间戳
        [Required]
        public DateTime LastActivityTime { get; set; }

        // 用户返回并恢复活动的时间
        public DateTime? ReturnTime { get; set; }

        // 以秒为单位的离开持续时间
        public int AfkDurationSeconds { get; set; }
    }
}
