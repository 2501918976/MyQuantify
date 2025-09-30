using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Models.Day
{
    public class DashboardData
    {
        public CardData Cards { get; set; }
        public MixedChartData MixedChart { get; set; }
        public List<PieItem> TimePie { get; set; }
        public List<PieItem> AppPie { get; set; }
    }
}
