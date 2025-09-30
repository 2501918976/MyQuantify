using MyQuantifyApp.DataCollector.Utilities;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
// 依赖于 PInvokeHelper 提供的 Win32 结构体和核心常量
using static MyQuantifyApp.DataCollector.Utilities.PInvokeHelper;

namespace MyQuantifyApp.DataCollector.Services
{
    /// <summary>
    /// 负责在 STA 线程上创建隐藏窗口，并监听 WM_CLIPBOARDUPDATE 消息。
    /// </summary>
    public sealed class ClipboardMonitor : IDisposable
    {
        public event EventHandler ClipboardUpdated;

        private Thread _staThread;
        private IntPtr _hWnd = IntPtr.Zero;
        private WndProc _wndProcDelegate; // 必须保持此委托的强引用，防止垃圾回收
        private const string WindowClassName = "ClipboardMonitorClass";

        /// <summary>
        /// 开始剪贴板监听器，启动 STA 线程。
        /// </summary>
        public void Start()
        {
            // 剪贴板 API 必须在 STA 线程上访问，所以使用 STA 线程启动消息循环
            _staThread = new Thread(MonitorLoop);
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.IsBackground = true;
            _staThread.Start();
        }

        /// <summary>
        /// 线程主循环：注册窗口类，创建隐藏窗口，并运行消息循环。
        /// </summary>
        private void MonitorLoop()
        {
            try
            {
                // 1. 注册窗口类
                _wndProcDelegate = new WndProc(WndProcHandler);

                // 关键修复: 使用 GetModuleHandle(null) 获取进程的模块实例句柄，这是最可靠的方法。
                // IntPtr hInstance = GetModuleHandle(null);
                IntPtr hInstance = GetModuleHandle(null);
                var wcx = new WNDCLASSEX
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                    // 使用 CS_HREDRAW | CS_VREDRAW 样式
                    style = CS_HREDRAW | CS_VREDRAW,
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                    cbClsExtra = 0,
                    cbWndExtra = 0,
                    hInstance = hInstance,
                    hIcon = IntPtr.Zero,
                    hCursor = IntPtr.Zero,
                    hbrBackground = IntPtr.Zero,
                    lpszMenuName = null,
                    lpszClassName = WindowClassName,
                    hIconSm = IntPtr.Zero
                };

                // 注册窗口类
                ushort atom = RegisterClassEx(ref wcx);
                if (atom == 0)
                {
                    Console.WriteLine($"[ClipboardMonitor] 消息循环异常: 无法注册窗口类。Win32 Error: {Marshal.GetLastWin32Error()}");
                    return;
                }

                // 2. 创建隐藏窗口 (使用 HWND_MESSAGE 创建消息-only窗口)
                _hWnd = CreateWindowEx(
                    0,                          // dwExStyle
                    WindowClassName,            // lpClassName
                    "Clipboard Monitor Window", // lpWindowName
                    0,                          // dwStyle (0 表示没有可见样式)
                    0, 0, 0, 0,                 // x, y, nWidth, nHeight
                    HWND_MESSAGE,            // HWND_MESSAGE: 消息-only窗口的父句柄
                    IntPtr.Zero,                // hMenu
                    hInstance,                  // hInstance
                    IntPtr.Zero);               // lpParam

                if (_hWnd == IntPtr.Zero)
                {
                    Console.WriteLine($"[ClipboardMonitor] 消息循环异常: 无法创建窗口。Win32 Error: {Marshal.GetLastWin32Error()}");
                    return;
                }

                Console.WriteLine($"[ClipboardMonitor] 消息窗口已创建，句柄: {_hWnd}");

                // 3. 注册剪贴板更新通知
                if (!AddClipboardFormatListener(_hWnd))
                {
                    Console.WriteLine($"[ClipboardMonitor] 警告: 无法添加剪贴板格式监听器。Win32 Error: {Marshal.GetLastWin32Error()}");
                }

                // 4. 运行消息循环
                // 4. 运行消息循环 (使用阻塞式的 GetMessage，这是最稳定可靠的方式)
                MSG msg;
                // 注意：GetMessage 的 hWnd 参数应该设为 IntPtr.Zero，
                // 因为 WM_CLIPBOARDUPDATE 消息是直接发送给窗口的，
                // 而 WM_QUIT 是发送给线程的消息队列的，IntPtr.Zero 可以处理这两者。
                // 虽然 GetMessage(out msg, _hWnd, 0, 0) 也能工作，但 GetMessage(out msg, IntPtr.Zero, 0, 0) 更常用。
                // 我们保留您之前的 GetMessage 写法，因为它在 STA 线程中能正确工作。
                while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
                {
                    // 在消息窗口场景中，通常不需要 TranslateMessage，
                    // 但保留它没有坏处。
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                // 增强异常日志
                Console.WriteLine($"[ClipboardMonitor] 消息循环崩溃: {ex.ToString()}");
            }
            finally
            {
                // 确保资源释放
                if (_hWnd != IntPtr.Zero)
                {
                    if (!DestroyWindow(_hWnd))
                    {
                        Console.WriteLine($"[ClipboardMonitor] 窗口销毁失败: {Marshal.GetLastWin32Error()}");
                    }
                    _hWnd = IntPtr.Zero;
                }

                // 添加线程状态日志
                Console.WriteLine($"[ClipboardMonitor] 消息循环退出，线程状态: {(_staThread?.IsAlive == true ? "存活" : "终止")}");
            }
        }

        /// <summary>
        /// 窗口过程处理器，接收来自 Windows 的消息。
        /// </summary>
        private IntPtr WndProcHandler(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_CLIPBOARDUPDATE:
                    Console.WriteLine("[ClipboardMonitor] 检测到剪贴板更新");
                    ClipboardUpdated?.Invoke(this, EventArgs.Empty);
                    break;

                case WM_DESTROY:
                    Console.WriteLine("[ClipboardMonitor] 收到销毁指令");
                    PostQuitMessage(0); // 显式发送退出信号
                    break;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
        // 新增 Win32 API
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin,
            uint wMsgFilterMax, uint wRemoveMsg);
        private const uint PM_REMOVE = 0x0001;
        private const uint WM_QUIT = 0x0012;
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);
        /// <summary>
        /// 清理资源：停止线程，销毁窗口。
        /// </summary>
        public void Dispose()
        {
            if (_hWnd != IntPtr.Zero)
            {
                // 1. 先移除监听器
                RemoveClipboardFormatListener(_hWnd);

                // 2. 向窗口发送销毁消息，从而退出 GetMessage 循环
                PostMessage(_hWnd, WM_DESTROY, IntPtr.Zero, IntPtr.Zero);

                // 3. 尝试等待线程安全退出
                if (_staThread != null && _staThread.IsAlive)
                {
                    _staThread.Join(500); // 最多等待 500ms
                }
            }
            // 允许 GC 清理委托
            _wndProcDelegate = null;
        }

        #region Win32 API 引入 (内部使用)

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        #endregion
    }
}
