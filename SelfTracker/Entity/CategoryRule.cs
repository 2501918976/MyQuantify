using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity
{
    public class CategoryRule
    {
        public int Id { get; set; }
        public string? ProcessName { get; set; }
        public string? ActivityType { get; set; } // 用户设定的类别
    }
}
