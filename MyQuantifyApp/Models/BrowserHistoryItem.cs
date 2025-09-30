using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Models
{
    // 浏览器历史记录，这通常是批量采集而不是实时事件
    public class BrowserHistoryItem
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTimeOffset LastVisited { get; set; }
        public int VisitCount { get; set; }
    }
}
