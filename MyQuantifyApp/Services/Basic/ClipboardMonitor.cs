using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MyQuantifyApp.Services.Native.NativeMethods;
using System.Threading;
using System.Drawing;
using MyQuantifyApp.Service;

namespace MyQuantifyApp.Service.Services
{
    /// <summary>
    /// 专用于管理消息专用窗口和剪贴板监听的服务。
    /// 运行在独立的 STA 线程中，以处理 WM_CLIPBOARDUPDATE 消息。
    /// </summary>
    public class ClipboardMonitor : IDisposable
    {
        public event EventHandler<string> ClipboardContentChanged;

        private static ClipboardMonitor _instance;
        private const string WindowClassName = "ClipboardMessageOnlyWindow";
        private IntPtr _messageWindowHandle = IntPtr.Zero;
        private Thread _monitorThread = null;
        private readonly WndProc _wndProcDelegate;

        public ClipboardMonitor()
        {
            // 初始化时确保委托被创建并指向静态 WndProc
            _wndProcDelegate = WndProcImplementation;
            _instance = this; // 在实例化时设置静态引用
        }

        /// <summary>
        /// 启动剪贴板监控服务。
        /// </summary>
        public void Start()
        {
            if (_monitorThread != null && _monitorThread.IsAlive)
            {
                return;
            }

            // 在一个新的 STA 线程中启动消息循环和窗口创建。
            _monitorThread = new Thread(MonitorThreadStart)
            {
                IsBackground = true
            };
            _monitorThread.SetApartmentState(ApartmentState.STA);
            _monitorThread.Start();
        }

        /// <summary>
        /// 线程入口点：负责窗口注册、创建和消息循环。
        /// </summary>
        private void MonitorThreadStart()
        {
            try
            {
                // 1. 注册窗口类
                RegisterMessageWindowClass();

                // 2. 创建消息专用窗口 (HWND_MESSAGE 作为父句柄)
                _messageWindowHandle = CreateWindowEx(
                    0,
                    WindowClassName,
                    null,
                    0,
                    0, 0, 0, 0,
                    HWND_MESSAGE,
                    IntPtr.Zero,
                    GetModuleHandle(null),
                    IntPtr.Zero);

                if (_messageWindowHandle == IntPtr.Zero)
                {
                    Console.WriteLine($"[ClipboardMonitor] 创建消息窗口失败: {Marshal.GetLastWin32Error()}");
                    return;
                }

                // 3. 注册剪贴板监听器
                if (!AddClipboardFormatListener(_messageWindowHandle))
                {
                    Console.WriteLine($"[ClipboardMonitor] 注册剪贴板监听器失败: {Marshal.GetLastWin32Error()}");
                    DestroyWindow(_messageWindowHandle);
                    _messageWindowHandle = IntPtr.Zero;
                    return;
                }

                // 4. 进入消息循环 (Message Loop)
                MSG msg;
                // GetMessage 返回 0 表示收到 WM_QUIT，返回 -1 表示错误
                while (GetMessage(out msg, _messageWindowHandle, 0, 0) > 0)
                {
                    DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClipboardMonitor] 监控线程发生错误: {ex.Message}");
            }
            finally
            {
                _messageWindowHandle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 注册窗口类。
        /// </summary>
        private void RegisterMessageWindowClass()
        {
            WNDCLASSEX wcx = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                hInstance = GetModuleHandle(null),
                lpszClassName = WindowClassName
            };

            // 注册窗口类
            RegisterClassEx(ref wcx);
        }

        /// <summary>
        /// 静态窗口过程函数，用于接收系统消息。
        /// </summary>
        private static IntPtr WndProcImplementation(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                // 剪贴板变化事件
                if (_instance != null)
                {
                    // 使用 NativeMethods 中 STA 安全的 GetClipboardText 辅助方法
                    string content = GetClipboardText();
                    if (!string.IsNullOrEmpty(content))
                    {
                        _instance.ClipboardContentChanged?.Invoke(_instance, content);
                    }
                }
            }
            else if (msg == WM_DESTROY)
            {
                // 收到 WM_DESTROY 时，发送 WM_QUIT 退出消息循环
                PostQuitMessage(0);
                return IntPtr.Zero;
            }

            // 对于不处理的消息，转发给系统默认的窗口过程
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);


        /// <summary>
        /// 停止服务，清理窗口和线程。
        /// </summary>
        public void Stop()
        {
            if (_messageWindowHandle != IntPtr.Zero)
            {
                // 1. 移除剪贴板监听器
                RemoveClipboardFormatListener(_messageWindowHandle);

                // 2. 发送 WM_DESTROY 消息，这将触发 WM_QUIT 退出消息循环
                PostMessage(_messageWindowHandle, WM_DESTROY, IntPtr.Zero, IntPtr.Zero);

                // 等待线程退出
                _monitorThread?.Join(1000);
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
