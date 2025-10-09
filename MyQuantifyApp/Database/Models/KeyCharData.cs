using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Database.Models
{
    public class KeyCharData
    {
        public int Id { get; set; }
        public int? WindowActivityId { get; set; }
        public string KeyChar { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}
