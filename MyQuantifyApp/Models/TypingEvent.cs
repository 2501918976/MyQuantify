using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Models
{
    // 打字事件记录
    public class TypingEvent : ActivityRecord
    {
        public override ActivityType Type => ActivityType.Typing;

        // 本次事件中记录的按键数量
        public int KeyCount { get; set; }
    }
}
