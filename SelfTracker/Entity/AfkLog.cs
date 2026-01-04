using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity
{
    /// <summary>
    /// 离开/挂机时间记录
    /// </summary>
    public class AfkLog
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // V2 核心字段
        public int? SessionId { get; set; }

        public double DurationMinutes => (EndTime - StartTime).TotalMinutes;
    }
}
