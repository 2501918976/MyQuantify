using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity.Base
{
    public class CopyLog
    {
        [Key]
        public int Id { get; set; }

        public int CopyCount { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int Duration { get; set; }

        // 进程
        public int? ProcessInfoId { get; set; }
        [ForeignKey(nameof(ProcessInfoId))]
        public virtual ProcessInfo? Process { get; set; }

        // 时间轴
        public int SystemStateLogId { get; set; }
        [ForeignKey(nameof(SystemStateLogId))]
        public virtual SystemStateLog Session { get; set; } = null!;
    }

}
