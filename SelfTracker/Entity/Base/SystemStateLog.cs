using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity.Base
{
    /// <summary>
    /// 系统状态表：记录设备宏观运行状态
    /// </summary>
    public class SystemStateLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 跨系统 / 展示用 SessionKey（非外键）
        /// </summary>
        public string? SessionKey { get; set; }

        [Required]
        public SystemStateType Type { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int Duration { get; set; }

        public string? Location { get; set; }
        public string? DeviceName { get; set; }

        // ===== 行为日志 =====
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        public virtual ICollection<TypingLog> TypingLogs { get; set; } = new List<TypingLog>();
        public virtual ICollection<CopyLog> CopyLogs { get; set; } = new List<CopyLog>();
    }


    public enum SystemStateType
    {
        Unknown = 0,
        PowerSession = 1,
        Sleep = 2,
        Hibernate = 3,
        AFK = 4,
        ActiveUsing = 5
    }

}
