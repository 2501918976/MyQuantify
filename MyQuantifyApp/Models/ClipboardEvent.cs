using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Models
{
    // 复制事件记录
    public class ClipboardEvent : ActivityRecord
    {
        public override ActivityType Type => ActivityType.ClipboardCopy;

        // 剪贴板内容类型（例如：Text、Image、Files）
        public string DataFormat { get; set; } = "Text";

        // 复制数据的大小（以字节为单位，如果是文件/图片等）
        public long DataSize { get; set; }
    }
}
