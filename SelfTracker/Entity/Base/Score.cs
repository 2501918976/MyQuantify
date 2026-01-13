using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Entity.Base
{
    /// <summary>
    /// 得分实体
    /// </summary>
    public class Score
    {
        public int Id { get; set; }

        public DateTime Time { get; set; }

        public int EfficiencyScore { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
