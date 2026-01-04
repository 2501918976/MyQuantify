using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity
{

    /// <summary>
    /// 开机/关机日志，用于计算总时长
    /// </summary>
    public class SystemSession
    {
        public int Id { get; set; }
        // 对应数据库的 start_time
        public DateTime StartTime { get; set; }
        // 对应数据库的 end_time
        public DateTime? EndTime { get; set; }
    }
}
