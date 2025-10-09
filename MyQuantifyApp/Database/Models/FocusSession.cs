using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Models
{
    public class FocusSession
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int FocusDuration { get; set; } = 25;  // 分钟
        public int RestDuration { get; set; } = 5;    // 分钟

        public string Status { get; set; } = "Idle";  // Idle, Running, Paused, Completed, Canceled
        public bool IsCompleted { get; set; } = false;
    }

}
