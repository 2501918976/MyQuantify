using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Models.Day
{
    public class CardData
    {
        // 单位：小时
        public double TotalTime { get; set; }
        public double WorkTime { get; set; }
        public double GameTime { get; set; }
        public double AfkTime { get; set; }
        // 单位：千字
        public double TypingCount { get; set; }
        public double CopyCount { get; set; }
    }
}
