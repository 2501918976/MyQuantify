using Microsoft.EntityFrameworkCore;
using SelfTracker.DataCollectors;
using SelfTracker.Entity.Base;
using SelfTracker.Repository.Base;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SelfTracker.Controllers
{
    /// <summary>
    /// 前台窗口收集控制器：获取当前用户活跃的窗口信息，并记录为 ActivityLog
    /// </summary>
    public class ForegroundController
    {
        private readonly ActivityLogRepository _activityRepo;
        private readonly ProcessInfoRepository _processRepo;
        private readonly CategoryMatcher _matcher;

        public ForegroundController(
            ActivityLogRepository activityRepo,
            ProcessInfoRepository processRepo,
            CategoryMatcher matcher)
        {
            _activityRepo = activityRepo;
            _processRepo = processRepo;
            _matcher = matcher;
        }

        public void CaptureCurrentActivity(SystemStateLog systemState)
        {
            var info = GetCurrentInfo();
            if (info == null) return;

            string processName = info.Value.ProcessName;
            string windowTitle = info.Value.WindowTitle;

            // 查找或创建进程
            var process = _processRepo.GetAll().FirstOrDefault(p => p.ProcessName == processName);
            if (process == null)
            {
                process = new ProcessInfo { ProcessName = processName };
                _processRepo.Add(process);
            }

            // 使用 CategoryMatcher 匹配分类
            var category = _matcher.MatchCategory(processName, windowTitle);
            if (category != null)
            {
                process.CategoryId = category.Id;
                _processRepo.Update(process);
            }

            // 创建 ActivityLog
            var activityLog = new ActivityLog
            {
                ProcessInfoId = process.Id,
                SystemStateLogId = systemState.Id,
                WindowTitle = windowTitle,
                StartTime = DateTime.Now,
                Duration = 0
            };

            _activityRepo.Add(activityLog);
        }

        private (string ProcessName, string WindowTitle)? GetCurrentInfo()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            GetWindowThreadProcessId(hwnd, out uint pid);

            try
            {
                using var process = Process.GetProcessById((int)pid);
                string processName = process.ProcessName;
                string windowTitle = GetTitle(hwnd);
                return (processName, windowTitle);
            }
            catch
            {
                return null;
            }
        }

        private string GetTitle(IntPtr hwnd)
        {
            int length = GetWindowTextLength(hwnd);
            if (length == 0) return string.Empty;
            var sb = new StringBuilder(length + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        #region Win32 API
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        #endregion
    }
}
