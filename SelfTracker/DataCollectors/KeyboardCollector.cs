using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SelfTracker.DataCollectors
{
    /// <summary>
    /// 键盘采集器：利用低级键盘钩子 (Low-Level Keyboard Hook) 统计全系统的击键次数
    /// </summary>
    public class KeyboardCollector : IDisposable
    {
        /// <summary>
        /// 当前统计周期内的击键总数
        /// </summary>
        public int KeyCount { get; private set; }

        /// <summary>
        /// 每当有按键按下时触发的事件。
        /// 可用于 UI 层的实时反馈（如闪烁图标、波纹特效等）
        /// </summary>
        public event Action OnKeyPressed;

        private IntPtr _hookId = IntPtr.Zero; // 钩子句柄
        private LowLevelKeyboardProc _proc;   // 必须持有该委托的引用，防止被 GC (垃圾回收)

        /// <summary>
        /// 启动全局键盘监听
        /// </summary>
        public void Start()
        {
            if (_hookId != IntPtr.Zero) return;

            // 将回调方法赋值给委托变量，确保在监听期间该委托不会被回收
            _proc = HookCallback;
            // 安装钩子
            _hookId = SetHook(_proc);
        }

        /// <summary>
        /// 核心方法：提取当前计数值并清零。
        /// 通常由定时器调用，将数据持久化到数据库后开始新一轮统计。
        /// </summary>
        /// <returns>返回清零前的计数值</returns>
        public int FlushCount()
        {
            int current = KeyCount;
            KeyCount = 0;
            return current;
        }

        /// <summary>
        /// 停止监听并注销钩子
        /// </summary>
        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _proc = null;
            }
        }

        /// <summary>
        /// 内部方法：安装 Windows 全局钩子
        /// </summary>
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                // 参数说明：
                // WH_KEYBOARD_LL (13): 低级键盘钩子
                // proc: 消息处理的回调函数
                // GetModuleHandle: 当前模块的句柄
                // 0: 表示监听所有线程的输入
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// 钩子回调函数：当系统发生键盘操作时，Windows 会调用此方法
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // 消息常量定义
            const int WM_KEYDOWN = 0x0100;    // 普通按键按下
            const int WM_SYSKEYDOWN = 0x0104; // 系统按键按下 (如 Alt + 键)

            // nCode >= 0 表示需要处理该消息
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                // 击键次数自增
                KeyCount++;

                // 触发事件通知外界（注意：回调运行在系统钩子链中，不宜执行耗时操作）
                OnKeyPressed?.Invoke();
            }

            // 将消息传递给钩子链中的下一个监听者（必须调用，否则会导致系统键盘卡死）
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() => Stop();

        #region Win32 API 导入

        private const int WH_KEYBOARD_LL = 13;

        // 定义处理函数委托
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // 安装钩子
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        // 卸载钩子
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        // 调用下一个钩子
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        // 获取模块句柄
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}