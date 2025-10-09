using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Models.Aggre
{
    public class DailySummary
    {
        public string Date { get; set; } = string.Empty;
        public int KeyCount { get; set; }
        public int CopyCount { get; set; }
        public int AfkSeconds { get; set; }
        public int WorkSeconds { get; set; }
        public int GameSeconds { get; set; }
        public int TotalActiveSeconds { get; set; }
    }
}
