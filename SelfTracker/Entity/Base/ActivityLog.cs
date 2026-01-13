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
    /// 活动日志表：记录每次活动的详细信息
    /// <summary>
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        public string? WindowTitle { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int Duration { get; set; } // 秒（派生字段）

        // ===== 进程 =====
        public int ProcessInfoId { get; set; }
        [ForeignKey(nameof(ProcessInfoId))]
        public virtual ProcessInfo Process { get; set; } = null!;

        // ===== 时间轴主表 =====
        public int SystemStateLogId { get; set; }
        [ForeignKey(nameof(SystemStateLogId))]
        public virtual SystemStateLog Session { get; set; } = null!;
    }

}
