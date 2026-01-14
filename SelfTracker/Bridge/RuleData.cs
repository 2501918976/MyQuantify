using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Bridge
{
    public class RuleData
    {
        public int? Id { get; set; }
        public string ProcessName { get; set; }
        public string TitleMatchValue { get; set; }
        public int CategoryId { get; set; }
        public int LogicType { get; set; }
        public int TitleMatchType { get; set; }
        public bool IsEnabled { get; set; }
    }
}
