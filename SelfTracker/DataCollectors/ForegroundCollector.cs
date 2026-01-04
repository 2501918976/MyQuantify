using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SelfTracker.DataCollectors
{
    /// <summary>
    /// 前台窗口收集器：用于追踪用户当前正在使用的应用程序及其窗口信息
    /// </summary>
    public class ForegroundCollector
    {
        /// <summary>
        /// 获取当前前台窗口的详细信息（元组形式返回），并自动判定活动类型
        /// </summary>
        /// <returns>
        /// 如果成功，返回包含 (进程名, 窗口标题, 判定后的活动类型) 的元组；
        /// 如果失败或没有前台窗口，返回 null。
        /// </returns>
        public (string ProcessName, string WindowTitle, string ActivityType)? GetCurrentInfo()
        {
            // 1. 获取当前处于屏幕最前端的窗口句柄
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;

            // 2. 获取该窗口所属的进程 ID (PID)
            GetWindowThreadProcessId(hwnd, out uint pid);

            try
            {
                // 3. 根据 PID 获取 .NET 的 Process 对象
                using (var process = Process.GetProcessById((int)pid))
                {
                    string processName = process.ProcessName; // 进程名（不含 .exe）
                    string windowTitle = GetTitle(hwnd);     // 窗口标题文字

                    // 4. 核心逻辑：根据进程名和标题实时判定活动类型（用于报表分类统计）
                    string activityType = GetActivityType(processName, windowTitle);

                    return (processName, windowTitle, activityType);
                }
            }
            catch (Exception)
            {
                // 某些系统进程或已关闭的进程可能导致访问拒绝或异常
                return null;
            }
        }

        /// <summary>
        /// 内部逻辑：根据预定义的规则，将进程映射为人类可读的活动类型
        /// </summary>
        /// <param name="processName">进程名</param>
        /// <param name="title">窗口标题（可用于更细化的判定）</param>
        private string GetActivityType(string processName, string title)
        {
            processName = processName.ToLower();

            // --- 简单的分类匹配逻辑（可在此扩展你的分类规则） ---

            // 开发工具：VS, VS Code, Rider
            if (processName.Contains("devenv") || processName.Contains("code") || processName.Contains("rider"))
                return "Development";

            // 浏览器：Chrome, Edge, Firefox
            if (processName.Contains("chrome") || processName.Contains("msedge") || processName.Contains("firefox"))
                return "Browsing";

            // 文件管理：Windows 资源管理器
            if (processName.Contains("explorer"))
                return "File Management";

            // 终端：PowerShell, CMD
            if (processName.Contains("powershell") || processName.Contains("cmd"))
                return "Terminal";

            // 其他默认归类
            return "General";
        }

        /// <summary>
        /// 辅助方法：通过窗口句柄获取其标题字符串
        /// </summary>
        private string GetTitle(IntPtr hwnd)
        {
            // 先获取标题的字符长度，以便分配内存缓冲区
            int length = GetWindowTextLength(hwnd);
            if (length == 0) return string.Empty;

            // 使用 StringBuilder 接收系统填充的字符串
            var sb = new StringBuilder(length + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        #region Win32 API 导入

        // 获取当前激活窗口句柄
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // 获取窗口对应的进程ID
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

        // 获取窗口标题内容
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // 获取窗口标题长度
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        #endregion
    }
}