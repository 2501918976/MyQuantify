using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Models
{
    // 所有活动记录的基类
    public abstract class ActivityRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // 唯一标识符
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now; // 活动发生的时间
        public abstract ActivityType Type { get; } // 抽象属性，强制子类定义类型

        // 当前活动发生时，哪个窗口处于聚焦状态
        public string? TargetApplication { get; set; }
        public string? TargetWindowTitle { get; set; }
    }
}
