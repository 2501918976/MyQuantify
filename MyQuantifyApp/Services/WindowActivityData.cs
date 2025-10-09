using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Services
{
    /// <summary>
    /// 内存中的窗口活动记录（缓冲用，不直接对应数据库）
    /// </summary>
    public class WindowActivityData
    {
        /// <summary>
        /// 对应的窗口主键 ID（来自 Window 表）
        /// </summary>
        public int WindowId { get; set; }

        /// <summary>
        /// 活动开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 活动结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 持续时间（秒）
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// 是否已同步到数据库
        /// </summary>
        public bool IsFlushed { get; set; } = false;
    }
}
