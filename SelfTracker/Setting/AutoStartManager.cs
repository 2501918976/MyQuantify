using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Setting
{
    public static class AutoStartManager
    {
        private const string RunKey =
            @"Software\Microsoft\Windows\CurrentVersion\Run";

        private const string AppName = "SelfTracker";

        public static void Enable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            key.SetValue(AppName, Assembly.GetExecutingAssembly().Location);
        }

        public static void Disable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            key.DeleteValue(AppName, false);
        }

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key.GetValue(AppName) != null;
        }
    }
}
