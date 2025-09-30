using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text; // 支持 GetWindowText
using System.Threading;
using System.Windows.Forms;

namespace MyQuantifyApp.DataCollector.Utilities
{
    /// <summary>
    /// 封装了与 Windows API 交互的 P/Invoke 方法、结构体、委托和常量。
    /// 提供对系统底层功能（如窗口、剪贴板、键盘和输入时间）的访问。
    /// </summary>
    public static class PInvokeHelper
    {
        // =====================================================
        // 核心句柄获取 P/Invoke
        // =====================================================

        /// <summary>
        /// 检索指定模块的句柄。用于获取当前进程或 DLL 的实例句柄 (hInstance)。
        /// </summary>
        /// <param name="lpModuleName">要获取其句柄的模块名称（通常为 null 表示当前进程）。</param>
        /// <returns>模块句柄。</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // =====================================================
        // 窗口信息 P/Invoke (ActivitySessionCollectorService 依赖)
        // =====================================================

        /// <summary>
        /// 检索前台窗口（用户当前正在操作的窗口）的句柄。
        /// </summary>
        /// <returns>前台窗口的句柄。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// 检索创建指定窗口的线程和进程的标识符。
        /// </summary>
        /// <param name="hWnd">窗口句柄。</param>
        /// <param name="lpdwProcessId">接收进程标识符的变量指针。</param>
        /// <returns>创建窗口的线程标识符。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// 复制指定窗口的标题栏文本（如果存在）。
        /// </summary>
        /// <param name="hWnd">包含文本的窗口或控件的句柄。</param>
        /// <param name="lpString">接收文本的 StringBuilder 对象。</param>
        /// <param name="nMaxCount">要复制的最大字符数。</param>
        /// <returns>复制的字符数。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// 确定指定窗口是否最大化。
        /// </summary>
        /// <param name="hWnd">窗口句柄。</param>
        /// <returns>如果窗口已最大化，则为 true；否则为 false。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        // =====================================================
        // 剪贴板监听 P/Invoke
        // =====================================================

        /// <summary>
        /// 将指定的窗口添加到剪贴板格式监听器列表中。
        /// </summary>
        /// <param name="hwnd">要添加到监听器列表的窗口句柄。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// 从剪贴板格式监听器列表中移除指定的窗口。
        /// </summary>
        /// <param name="hwnd">要移除的窗口句柄。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// 创建一个扩展窗口（如消息-only窗口）。
        /// </summary>
        /// <param name="dwExStyle">扩展窗口样式。</param>
        /// <param name="lpClassName">窗口类名。</param>
        /// <param name="lpWindowName">窗口名称。</param>
        /// <param name="dwStyle">窗口样式。</param>
        /// <param name="x">窗口的初始 X 坐标。</param>
        /// <param name="y">窗口的初始 Y 坐标。</param>
        /// <param name="nWidth">窗口的初始宽度。</param>
        /// <param name="nHeight">窗口的初始高度。</param>
        /// <param name="hWndParent">父窗口或所有者窗口的句柄（消息-only窗口使用 HWND_MESSAGE）。</param>
        /// <param name="hMenu">菜单句柄或子窗口 ID。</param>
        /// <param name="hInstance">实例句柄。</param>
        /// <param name="lpParam">创建参数。</param>
        /// <returns>新窗口的句柄。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        /// <summary>
        /// 注册一个窗口类以供后续的 CreateWindowEx 调用使用。
        /// </summary>
        /// <param name="lpwcx">指向 WNDCLASSEX 结构的指针。</param>
        /// <returns>成功注册的类原子。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        /// <summary>
        /// 从调用线程的消息队列中检索消息。如果队列为空，则等待消息。
        /// </summary>
        /// <param name="lpMsg">接收消息信息的 MSG 结构。</param>
        /// <param name="hWnd">要检索消息的窗口句柄（IntPtr.Zero 表示所有窗口）。</param>
        /// <param name="wMsgFilterMin">要检索的最小消息值。</param>
        /// <param name="wMsgFilterMax">要检索的最大消息值。</param>
        /// <returns>非零值表示有消息，0 表示收到 WM_QUIT。</returns>
        [DllImport("user32.dll")]
        public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        /// <summary>
        /// 将消息分派给窗口过程 (WndProc)。
        /// </summary>
        /// <param name="lpmsg">包含消息的 MSG 结构。</param>
        /// <returns>窗口过程返回的值。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        /// <summary>
        /// 将消息放置到指定窗口的消息队列中，并立即返回。
        /// </summary>
        /// <param name="hWnd">目标窗口句柄。</param>
        /// <param name="Msg">要发送的消息。</param>
        /// <param name="wParam">消息的第一个参数。</param>
        /// <param name="lParam">消息的第二个参数。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 包含窗口类信息。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc; // 窗口过程函数指针
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        /// <summary>
        /// 包含来自线程消息队列的消息信息。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point p;
            public int lPrivate;
        }

        /// <summary>
        /// 窗口过程委托定义，用于处理窗口接收到的消息。
        /// </summary>
        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // *** 窗口类样式常量 ***
        /// <summary> 窗口水平调整大小时重绘。 </summary>
        public const uint CS_HREDRAW = 0x0002;
        /// <summary> 窗口垂直调整大小时重绘。 </summary>
        public const uint CS_VREDRAW = 0x0001;
        // **********************************************

        /// <summary> 消息-only 窗口的父句柄常量。 </summary>
        public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        /// <summary> 剪贴板内容已更改的消息。 </summary>
        public const uint WM_CLIPBOARDUPDATE = 0x031D;
        /// <summary> 窗口被销毁的消息。 </summary>
        public const uint WM_DESTROY = 0x0002;
        /// <summary> 用于设置或获取与窗口关联的用户数据的偏移量。 </summary>
        public const uint GWLP_USERDATA = 0xFFFFFFFC;

        /// <summary>
        /// 安全读取剪贴板文本。由于剪贴板 API 必须在单线程单元 (STA) 模式下访问，因此会创建一个临时 STA 线程来执行读取。
        /// </summary>
        /// <returns>剪贴板中的文本内容，如果没有文本或读取失败，则返回空字符串。</returns>
        public static string GetClipboardText()
        {
            string content = string.Empty;

            Thread t = new Thread(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                        content = Clipboard.GetText();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PInvokeHelper] 读取剪贴板失败: {ex.Message}");
                }
            });

            t.SetApartmentState(ApartmentState.STA); // 必须是 STA 线程才能访问 System.Windows.Forms.Clipboard
            t.Start();
            t.Join(); // 等待线程完成

            return content;
        }

        // =====================================================
        // 键盘钩子 (TypingCountService 依赖)
        // =====================================================

        /// <summary> 低层级键盘钩子类型常量。 </summary>
        public const int WH_KEYBOARD_LL = 13;
        /// <summary> 高层级键盘钩子类型常量（可选）。 </summary>
        public const int WH_KEYBOARD = 2;

        /// <summary>
        /// 包含低层级键盘输入事件的信息。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;      // 虚拟键码
            public uint scanCode;    // 硬件扫描码
            public uint flags;       // 标志
            public uint time;        // 时间戳
            public IntPtr dwExtraInfo; // 额外信息
        }

        /// <summary>
        /// 安装应用程序定义的钩子过程到钩子链中。
        /// </summary>
        /// <param name="idHook">钩子类型（如 WH_KEYBOARD_LL）。</param>
        /// <param name="lpfn">指向钩子过程的指针。</param>
        /// <param name="hMod">包含钩子过程的 DLL 模块句柄。</param>
        /// <param name="dwThreadId">与钩子过程关联的线程标识符（0 表示全局）。</param>
        /// <returns>钩子过程句柄。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>
        /// 从当前钩子链中移除钩子过程。
        /// </summary>
        /// <param name="hhk">要移除的钩子句柄。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// 将钩子信息传递给当前钩子链中的下一个钩子过程。
        /// </summary>
        /// <param name="hhk">当前钩子句柄。</param>
        /// <param name="nCode">钩子代码。</param>
        /// <param name="wParam">消息参数。</param>
        /// <param name="lParam">消息参数。</param>
        /// <returns>下一个钩子过程返回的值。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 钩子过程委托定义。
        /// </summary>
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary> 按键按下消息。 </summary>
        public const uint WM_KEYDOWN = 0x0100;

        // =====================================================
        // AFK 检测 (ActivitySessionCollectorService 依赖)
        // =====================================================

        /// <summary>
        /// 检索上次输入事件（键盘或鼠标）的时间。
        /// </summary>
        /// <param name="plii">指向 LASTINPUTINFO 结构的指针。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// 包含上次输入事件的时间信息。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public uint cbSize; // 结构大小
            public uint dwTime; // 上次输入事件发生时的系统时间 (TickCount)
        }

        /// <summary>
        /// 计算系统空闲时间（以秒为单位）。
        /// </summary>
        /// <returns>自上次输入以来的秒数。</returns>
        public static uint GetIdleTimeInSeconds()
        {
            LASTINPUTINFO lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>();
            if (!GetLastInputInfo(ref lii))
                return 0;

            uint tickCount = (uint)Environment.TickCount;
            // 计算时间差并转换为秒
            return (tickCount - lii.dwTime) / 1000;
        }

        // =====================================================
        // 打字消息循环增加的内容
        // =====================================================

        /// <summary>
        /// 将虚拟键消息转换为字符消息。用于处理键盘输入。
        /// </summary>
        /// <param name="lpMsg">指向包含消息信息的 MSG 结构的指针。</param>
        /// <returns>如果消息被翻译（即产生了一个字符消息），则为 true。</returns>
        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        /// <summary>
        /// 将消息发布到指定线程的消息队列。
        /// </summary>
        /// <param name="idThread">目标线程标识符。</param>
        /// <param name="Msg">要发送的消息。</param>
        /// <param name="wParam">消息的第一个参数。</param>
        /// <param name="lParam">消息的第二个参数。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary> 退出线程消息循环的消息。 </summary>
        public const uint WM_QUIT = 0x0012;
    }
}