using MyQuantifyApp.Services.Basic;
using MyQuantifyApp.Services.Native;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MyQuantifyApp.Services.Native.NativeMethods;

namespace MyQuantifyApp.Service
{

    internal class ActiveWindowHook
    {
        private WinEventDelegate _winEventDelegate;
        private IntPtr _hookHandleTitleChange = IntPtr.Zero;
        private IntPtr _hookHandleWinChange = IntPtr.Zero;
        private string _lastWindowTitle;
        private Thread _messageLoopThread;
        private bool _running = false;

        public event EventHandler<ActiveWindowChangedEventArgs> ActiveWindowChanged;

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;

        public void Hook()
        {
            _winEventDelegate = WinEventProc;
            _running = true;

            // 启动消息循环线程
            _messageLoopThread = new Thread(MessageLoopThread)
            {
                IsBackground = true,
                Name = "ActiveWindowHookThread"
            };
            _messageLoopThread.Start();

            RaiseOne();
        }

        public void UnHook()
        {
            _running = false;

            if (_hookHandleWinChange != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hookHandleWinChange);
                _hookHandleWinChange = IntPtr.Zero;
            }

            if (_hookHandleTitleChange != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hookHandleTitleChange);
                _hookHandleTitleChange = IntPtr.Zero;
            }
        }

        private void MessageLoopThread()
        {
            _hookHandleWinChange = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

            _hookHandleTitleChange = NativeMethods.SetWinEventHook(EVENT_OBJECT_NAMECHANGE, EVENT_OBJECT_NAMECHANGE,
                IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

            // 👇 添加伪消息循环，保证线程持续接收事件
            NativeMessage msg;
            while (_running)
            {
                NativeMethods.GetMessage(out NativeMethods.MSG msg1, IntPtr.Zero, 0, 0);
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != NativeMethods.OBJID_WINDOW) return;

            var title = GetActiveWindowTitle(hwnd); 

            if (!string.IsNullOrEmpty(title) && _lastWindowTitle != title)
            {
                _lastWindowTitle = title;
                OnActiveWindowChanged(hwnd, title);
            }
        }

        private void OnActiveWindowChanged(IntPtr hwnd, string title)
        {
            // 1. 获取进程信息
            var (processName, filePath) = GetProcessInfo(hwnd);

            // 2. 触发事件，传递所有三个参数
            var handler = ActiveWindowChanged;
            handler?.Invoke(this, new ActiveWindowChangedEventArgs(title, processName, filePath));
        }

        public void RaiseOne()
        {
            var hwnd = NativeMethods.GetForegroundWindow();

            _lastWindowTitle = GetActiveWindowTitle(hwnd);

            if (!string.IsNullOrEmpty(_lastWindowTitle))
            {
                OnActiveWindowChanged(hwnd, _lastWindowTitle);
            }
        }


        private (string processName, string filePath) GetProcessInfo(IntPtr hwnd)
        {
            uint pid = 0;
            NativeMethods.GetWindowThreadProcessId(hwnd, out pid);

            if (pid == 0)
            {
                Log.Warning("无法获取窗口句柄 {Hwnd} 的进程ID。", hwnd);
                return ("UnknownProcess", "UnknownPath");
            }

            IntPtr processHandle = IntPtr.Zero;
            string filePath = "UnknownPath";
            string processName = "UnknownProcess";

            try
            {
                // 打开进程，需要查询信息和读取权限
                processHandle = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ, false, pid);

                if (processHandle != IntPtr.Zero)
                {
                    // 获取文件路径
                    var capacity = 1024;
                    var pathBuilder = new StringBuilder(capacity);

                    if (NativeMethods.GetModuleFileNameEx(processHandle, IntPtr.Zero, pathBuilder, capacity) > 0)
                    {
                        filePath = pathBuilder.ToString();
                        processName = System.IO.Path.GetFileName(filePath);
                    }
                    else
                    {
                        // 尝试获取 ProcessName 备用方法
                        try
                        {
                            var proc = Process.GetProcessById((int)pid);
                            processName = proc.ProcessName + ".exe";
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取进程信息失败，PID: {Pid}", pid);
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(processHandle);
                }
            }

            return (processName, filePath);
        }

        /// <summary>
        /// 获取当前活动窗口的标题。
        /// </summary>
        /// <param name="handle">可选：要获取标题的窗口句柄。如果为 default，则获取前台窗口。</param>
        public static string GetActiveWindowTitle(IntPtr handle = default)
        {
            // 如果未传入句柄，则获取前台窗口的句柄
            if (handle == default)
                handle = NativeMethods.GetForegroundWindow();

            const int nChars = 1024;
            var buff = new StringBuilder(nChars);
            return NativeMethods.GetWindowText(handle, buff, nChars) > 0 ? buff.ToString() : null;
        }

        // 👇 新增结构体 & 外部声明
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int pt_x;
            public int pt_y;
        }
    }

}