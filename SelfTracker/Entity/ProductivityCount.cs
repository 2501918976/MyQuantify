using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity
{
    /// <summary>
    /// 5分钟定点打字与复制统计
    /// </summary>
    public class ProductivityCount
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int Keystrokes { get; set; }
        public int CopyCount { get; set; }

        // V2 核心字段：关联当前是哪次开机，方便分段统计
        public int? SessionId { get; set; }

        // V2 核心字段：明确这组数据是从什么时候开始统计的，持续了多久
        public DateTime? PeriodStart { get; set; }
        public int PeriodSeconds { get; set; } = 60;
    }
}
