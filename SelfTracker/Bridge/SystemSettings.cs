using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Bridge
{
    public class SystemSettings
    {
        public int writeInterval { get; set; }
        public int afkTime { get; set; }
        public int filterTime { get; set; }
        public bool autoStart { get; set; }
        public bool minimizeToTray { get; set; }
        public bool showNotifications { get; set; }
    }
}
