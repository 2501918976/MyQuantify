using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Models
{
    // 定义所有可能的活动类型
    public enum ActivityType
    {
        Typing,           // 打字输入
        ClipboardCopy,    // 复制/剪切操作
        WindowFocus,      // 窗口焦点变化
        AfkStart,         // 开始离开（AFK）
        AfkEnd,           // 结束离开（回到活跃）
        BrowserVisit      // 浏览器历史记录访问
    }
}
