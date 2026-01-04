using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity
{
    /// <summary>
    /// 活动窗口与进程记录
    /// </summary>
    public class ActivityLog
    {
        public int Id { get; set; }
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Duration { get; set; }

        // V2 核心字段
        public int? SessionId { get; set; }
        public string ActivityType { get; set; } // 比如：Coding, Gaming, Browsing
    }
}




