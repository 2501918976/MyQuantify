using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Models
{
    public class ClipboardActivityData
    {
        public int Id { get; set; }
        public int? WindowActivityId { get; set; }
        public string Content { get; set; } = "";
        public int Length { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
