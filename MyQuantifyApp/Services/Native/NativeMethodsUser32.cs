using MyQuantifyApp.Service;
using MyQuantifyApp.Services.Enems;
using System;                            // 基础类型，如 IntPtr
using System.Drawing;                    // 引入 Point (用于 MSG 结构体)
using System.Runtime.InteropServices;    // 核心命名空间，用于 Platform Invoke (P/Invoke)
using System.Text;                       // 引入 StringBuilder，用于处理字符串缓冲区
using System.Threading;                  // 引入 Thread，用于 GetClipboardText 辅助方法
using System.Windows;
// ReSharper disable InconsistentNaming // 禁用 ReSharper 警告，允许使用与 Windows API 风格一致的命名（如首字母小写）

namespace MyQuantifyApp.Services.Native
{
    /// <summary>
    /// 静态类，用于声明和封装对 Windows API (user32.dll, kernel32.dll) 的调用。
    /// 此处集中了所有 Native 方法、结构体和常量定义。
    /// </summary>
    internal static partial class NativeMethods
    {
        // =====================================================
        // 结构体和委托定义 (Structures and Delegates)
        // =====================================================

        /// <summary>
        /// 包含窗口类信息。用于 RegisterClassEx 函数。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public nint lpfnWndProc; // 窗口过程函数指针
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpszClassName;
            public nint hIconSm;
        }

        /// <summary>
        /// 包含来自线程消息队列的消息信息。用于 GetMessage 函数。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public nint hwnd;
            public uint message;
            public nint wParam;
            public nint lParam;
            public uint time;
            public System.Drawing.Point p;
            public int lPrivate;
        }

        /// <summary>
        /// 窗口过程委托定义，用于处理窗口接收到的消息 (WndProc)。
        /// </summary>
        public delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

        /// <summary>
        /// 钩子过程的回调函数委托签名 (HookProc)。
        /// 必须匹配 SetWindowsHookEx 期望的函数签名。
        /// </summary>
        internal delegate nint HookProc(int code, nint wParam, nint lParam);

        /// <summary>
        /// WinEvent 钩子过程的回调函数委托签名 (WinEventDelegate)。
        /// 必须匹配 SetWinEventHook 期望的函数签名。
        /// </summary>
        internal delegate void WinEventDelegate(
            nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread,
            uint dwmsEventTime);


        // =====================================================
        // 常量和特殊值 (Constants)
        // =====================================================

        // *** 进程访问权限常量 (PROCESS_ACCESS_RIGHTS) ***
        /// <summary> 必需权限：查询进程信息 (PROCESS_QUERY_INFORMATION)。 </summary>
        public const uint PROCESS_QUERY_INFORMATION = 0x0400;

        /// <summary> 必需权限：读取进程的内存 (PROCESS_VM_READ)。 </summary>
        public const uint PROCESS_VM_READ = 0x0010;

        // *** 窗口对象 ID 常量 ***
        /// <summary> 窗口对象 ID (OBJID_WINDOW)。 </summary>
        public const int OBJID_WINDOW = 0x00000000;
        // **********************************************

        // *** 窗口类样式常量 ***
        /// <summary> 窗口水平调整大小时重绘。 </summary>
        public const uint CS_HREDRAW = 0x0002;
        /// <summary> 窗口垂直调整大小时重绘。 </summary>
        public const uint CS_VREDRAW = 0x0001;
        // **********************************************

        /// <summary> 消息-only 窗口的父句柄常量 (Message-Only Window)。 </summary>
        public static readonly nint HWND_MESSAGE = new nint(-3);

        /// <summary> 剪贴板内容已更改的消息 (WM_CLIPBOARDUPDATE)。 </summary>
        public const uint WM_CLIPBOARDUPDATE = 0x031D;
        /// <summary> 窗口被销毁的消息 (WM_DESTROY)。 </summary>
        public const uint WM_DESTROY = 0x0002;
        /// <summary> 退出线程消息循环的消息 (WM_QUIT)。 </summary>
        public const uint WM_QUIT = 0x0012;
        /// <summary> 用于设置或获取与窗口关联的用户数据的偏移量 (GWLP_USERDATA)。 </summary>
        public const uint GWLP_USERDATA = 0xFFFFFFFC;

        // *** WinEvent 钩子常量 ***
        /// <summary> 当一个新窗口到达前台时发送。 </summary>
        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

        /// <summary> 钩子回调在发起事件的线程之外被调用。 </summary>
        public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        // **********************************************


        // =======================================================
        // 模块、进程和窗口信息相关 P/Invoke
        // =======================================================

        /// <summary>
        /// [kernel32.dll] 打开现有的本地进程对象。
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        /// <summary>
        /// [psapi.dll] 检索指定进程的指定模块的完整路径。
        /// </summary>
        [DllImport("psapi.dll", CharSet = CharSet.Auto)]
        public static extern uint GetModuleFileNameEx(nint hProcess, nint hModule, StringBuilder lpFilename, int nSize);

        /// <summary>
        /// [kernel32.dll] 关闭一个打开的对象句柄。
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(nint hObject);
        /// <summary>
        /// [kernel32.dll] 检索指定模块的模块句柄。常用于 SetWindowsHookEx。
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern nint GetModuleHandle(string lpModuleName);

        /// <summary>
        /// [user32.dll] 检索前台窗口（用户当前正在使用的窗口）的句柄。
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint GetForegroundWindow();

        /// <summary>
        /// [user32.dll] 检索创建指定窗口的线程 ID，并可选地检索创建该窗口的进程 ID。
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        /// <summary>
        /// [user32.dll] 复制指定窗口的标题栏文本（如果它有的话）。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// [user32.dll] 确定指定的窗口是否最大化。
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(nint hWnd);


        // =======================================================
        // 键盘状态和转换相关 P/Invoke
        // =======================================================

        /// <summary>
        /// [user32.dll] 检索指定虚拟键的状态（按下/抬起，开关/锁定）。
        /// 用于确定 CTRL, SHIFT, CAPS LOCK 等键的状态。
        /// </summary>
        /// <param name="vKey">虚拟键码。</param>
        /// <returns>键的状态：负值表示键处于按下状态。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern short GetKeyState(int vKey);

        /// <summary>
        /// [user32.dll] 将指定的虚拟键码和键盘状态翻译成一个或多个 ASCII 字符。
        /// 这是将按键事件转换为实际字符的核心方法，用于处理键盘布局、大小写和死键。
        /// </summary>
        /// <param name="uVirtKey">虚拟键码。</param>
        /// <param name="uScanCode">硬件扫描码。</param>
        /// <param name="lpKeyState">包含 256 字节的键盘状态数组。</param>
        /// <param name="lpChar">用于接收转换后字符的 StringBuilder 缓冲区。</param>
        /// <param name="uFlags">标志（通常为 0）。</param>
        /// <returns>转换出的字符数，负值表示死键。</returns>
        [DllImport("user32.dll")]
        internal static extern int ToAscii(uint uVirtKey, uint uScanCode, byte[] lpKeyState, [Out] StringBuilder lpChar,
            uint uFlags);

        /// <summary>
        /// [user32.dll] 将虚拟键码或扫描码转换为另一个值（例如，虚拟键码到字符码）。
        /// 用于检查键是否为死键 (Dead Key)。
        /// </summary>
        /// <param name="uCode">要转换的键码（虚拟键码或扫描码）。</param>
        /// <param name="uMapType">转换的类型（来自 MapVirtualKeyMapTypes 枚举）。</param>
        /// <returns>转换结果。</returns>
        [DllImport("user32.dll")]
        internal static extern int MapVirtualKey(uint uCode, MapVirtualKeyMapTypes uMapType);


        // =======================================================
        // Windows 钩子 (Hook) 相关 P/Invoke
        // =======================================================

        /// <summary>
        /// [user32.dll] 安装应用程序定义的钩子过程到系统钩子链中。
        /// </summary>
        /// <param name="hookType">钩子的类型（如 WH_KEYBOARD_LL - 低级键盘钩子）。</param>
        /// <param name="lpfn">指向钩子过程的回调函数委托。</param>
        /// <param name="hMod">包含钩子过程的模块句柄（全局钩子需要）。</param>
        /// <param name="dwThreadId">与钩子过程关联的线程标识符（0 表示全局）。</param>
        /// <returns>钩子过程的句柄。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint SetWindowsHookEx(HookType hookType, HookProc lpfn, nint hMod, uint dwThreadId);

        /// <summary>
        /// [user32.dll] 将钩子信息传递给当前钩子链中的下一个钩子过程。
        /// 钩子回调函数必须调用此函数，除非它要阻止消息。
        /// </summary>
        /// <param name="hhk">当前钩子句柄。</param>
        /// <param name="nCode">钩子代码。</param>
        /// <param name="wParam">消息参数。</param>
        /// <param name="lParam">消息参数。</param>
        /// <returns>下一个钩子过程返回的值。</returns>
        [DllImport("user32.dll")]
        internal static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

        /// <summary>
        /// [user32.dll] 从当前钩子链中移除钩子过程。
        /// </summary>
        /// <param name="hhk">要移除的钩子句柄。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)] // 明确指定返回类型应被封送为 C# 的 bool
        internal static extern bool UnhookWindowsHookEx(nint hhk);


        // =======================================================
        // WinEvent 钩子相关 P/Invoke (窗口事件，如切换焦点)
        // =======================================================

        /// <summary>
        /// [user32.dll] 安装一个事件钩子函数来监视特定的事件范围。
        /// 用于监听窗口焦点变化、对象创建/销毁等辅助功能事件。
        /// </summary>
        /// <param name="eventMin">要监视的最小事件值。</param>
        /// <param name="eventMax">要监视的最大事件值。</param>
        /// <param name="hmodWinEventProc">处理事件的模块句柄。</param>
        /// <param name="lpfnWinEventProc">指向事件回调函数的委托。</param>
        /// <param name="idProcess">要监视的进程 ID（0 表示所有进程）。</param>
        /// <param name="idThread">要监视的线程 ID（0 表示所有线程）。</param>
        /// <param name="dwFlags">钩子标志。</param>
        /// <returns>事件钩子的句柄。</returns>
        [DllImport("user32.dll")]
        internal static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        /// <summary>
        /// [user32.dll] 移除由 SetWinEventHook 安装的事件钩子。
        /// </summary>
        /// <param name="hWinEventHook">要移除的事件钩子句柄。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnhookWinEvent(nint hWinEventHook);


        // =======================================================
        // 剪贴板监听和消息循环 P/Invoke
        // =======================================================

        /// <summary>
        /// [user32.dll] 将指定的窗口添加到剪贴板格式监听器列表中。
        /// 用于接收 WM_CLIPBOARDUPDATE 消息。
        /// </summary>
        /// <param name="hwnd">要添加到监听器列表的窗口句柄。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AddClipboardFormatListener(nint hwnd);

        /// <summary>
        /// [user32.dll] 从剪贴板格式监听器列表中移除指定的窗口。
        /// </summary>
        /// <param name="hwnd">要移除的窗口句柄。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveClipboardFormatListener(nint hwnd);

        /// <summary>
        /// [user32.dll] 创建一个扩展窗口（例如用于剪贴板监听的消息-only窗口）。
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
        public static extern nint CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            nint hWndParent,
            nint hMenu,
            nint hInstance,
            nint lpParam);

        /// <summary>
        /// [user32.dll] 注册一个窗口类以供后续的 CreateWindowEx 调用使用。
        /// </summary>
        /// <param name="lpwcx">指向 WNDCLASSEX 结构的指针。</param>
        /// <returns>成功注册的类原子。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        /// <summary>
        /// [user32.dll] 从调用线程的消息队列中检索消息。如果队列为空，则等待消息（阻塞）。
        /// 这是消息循环的核心函数。
        /// </summary>
        /// <param name="lpMsg">接收消息信息的 MSG 结构。</param>
        /// <param name="hWnd">要检索消息的窗口句柄（IntPtr.Zero 表示所有窗口）。</param>
        /// <param name="wMsgFilterMin">要检索的最小消息值。</param>
        /// <param name="wMsgFilterMax">要检索的最大消息值。</param>
        /// <returns>非零值表示有消息，0 表示收到 WM_QUIT。</returns>
        [DllImport("user32.dll")]
        public static extern int GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        /// <summary>
        /// [user32.dll] 将消息分派给窗口过程 (WndProc)。
        /// </summary>
        /// <param name="lpmsg">包含消息的 MSG 结构。</param>
        /// <returns>窗口过程返回的值。</returns>
        [DllImport("user32.dll")]
        public static extern nint DispatchMessage([In] ref MSG lpmsg);

        /// <summary>
        /// [user32.dll] 将消息放置到指定窗口的消息队列中，并立即返回（异步）。
        /// </summary>
        /// <param name="hWnd">目标窗口句柄。</param>
        /// <param name="Msg">要发送的消息。</param>
        /// <param name="wParam">消息的第一个参数。</param>
        /// <param name="lParam">消息的第二个参数。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll")]
        public static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

        /// <summary>
        /// [user32.dll] 将消息发布到指定线程的消息队列（异步）。
        /// 常用于发送 WM_QUIT 以停止消息循环。
        /// </summary>
        /// <param name="idThread">目标线程标识符。</param>
        /// <param name="Msg">要发送的消息。</param>
        /// <param name="wParam">消息的第一个参数。</param>
        /// <param name="lParam">消息的第二个参数。</param>
        /// <returns>如果成功，则为 true。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(uint idThread, uint Msg, nint wParam, nint lParam);


        // =====================================================
        // 辅助方法 (High-Level Helpers)
        // =====================================================

        /// <summary>
        /// 安全读取剪贴板文本。由于剪贴板 API 必须在单线程单元 (STA) 模式下访问，因此会创建一个临时 STA 线程来执行读取。
        /// 注意：这是一个高层级的辅助方法，通常应放在 PInvokeHelper 类中。
        /// </summary>
        /// <returns>剪贴板中的文本内容，如果没有文本或读取失败，则返回空字符串。</returns>
        /// <summary>
        /// 安全读取剪贴板文本。
        /// </summary>
        public static string GetClipboardText()
        {
            string content = string.Empty;

            Thread t = new Thread(() =>
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                        content = System.Windows.Clipboard.GetText();
                }
                catch (Exception ex)
                {
                    // 可替换为日志记录
                    Console.WriteLine($"[ClipboardHelper] 读取剪贴板失败: {ex.Message}");
                }
            });

            t.SetApartmentState(ApartmentState.STA); // WPF Clipboard 也需要 STA
            t.Start();
            t.Join();

            return content;
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TranslateMessage([In] ref MSG lpMsg);


    }
}
