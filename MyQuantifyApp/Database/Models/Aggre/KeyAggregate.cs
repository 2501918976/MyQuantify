using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Models.Aggre
{
    public class KeyAggregate
    {
        public string Date { get; set; }
        public string KeyChar { get; set; }
        public int Count { get; set; }
    }
}
