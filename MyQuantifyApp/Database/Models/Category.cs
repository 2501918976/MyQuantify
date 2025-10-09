using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public class ProcessInfo
    {
        public int Id { get; set; }
        public string ProcessName { get; set; } = "";
        public string? FilePath { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; } = "";
        public List<string> Windows { get; set; } = new();
    }

    public class WindowInfo
    {
        public int Id { get; set; }
        public string WindowTitle { get; set; } = "";
        public int ProcessId { get; set; }
        public int? CategoryId { get; set; }
    }

    public class WindowActivity
    {
        public int Id { get; set; }
        public int WindowId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationSeconds { get; set; }
    }

}
