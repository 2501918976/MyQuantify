using System; // 基础类型，如 IntPtr, EventHandler
using System.ComponentModel; // 引入 Win32Exception 类，用于处理 Win32 错误码
using System.Diagnostics; // 引入 Process 类，用于获取当前进程信息
using System.Runtime.InteropServices; // 引入 Marshal 类，用于处理非托管内存和结构体转换
using MyQuantifyApp.Services.Enems;
using MyQuantifyApp.Services.Native;
using MyQuantifyApp.Services.Other; // 引入封装 Windows API 的 NativeMethods 静态类

namespace MyQuantifyApp.Service.Services
{
    /// <summary>
    /// 封装了 Windows API 低级键盘钩子的安装、卸载和事件处理逻辑。
    /// </summary>
    internal class KeyboardHook
    {
        // =======================================================
        // Windows 消息常量 (wParam 参数)
        // =======================================================
        private const int WM_KEYDOWN = 0x100; // 普通按键按下消息
        private const int WM_SYSKEYDOWN = 0x104; // 系统按键按下消息 (例如 ALT + 键)
        private const int WM_KEYUP = 0x101; // 普通按键抬起消息
        private const int WM_SYSKEYUP = 0x105; // 系统按键抬起消息 (例如 ALT + 键)

        // =======================================================
        // 核心修正: 保留对钩子委托的引用以防止 GC 回收
        // =======================================================
        /// <summary> 
        /// 存储 HookProc 委托实例。必须保留此引用，否则 GC 可能会导致 System.ExecutionEngineException。
        /// </summary>
        private readonly NativeMethods.HookProc _procDelegate;

        // =======================================================
        // 成员变量
        // =======================================================
        private IntPtr _keyboardHookHandle; // 存储 SetWindowsHookEx 返回的钩子句柄 (Hook Handle)
        private readonly KeyProcessing _keyProcessing; // 核心按键处理对象，用于将虚拟键码转换为字符/字符串

        // =======================================================
        // 公共事件：将按键事件转发为更易用的字符串事件
        // =======================================================
        public event EventHandler<StringDownEventArgs> StringDown; // 按键按下后，经过 KeyProcessing 转换为字符串后的事件
        public event EventHandler<StringDownEventArgs> StringUp; // 按键抬起后，经过 KeyProcessing 转换为字符串后的事件

        /// <summary>
        /// 构造函数
        /// </summary>
        public KeyboardHook()
        {
            // 在构造函数中创建委托实例并保存，以防止 GC
            _procDelegate = KeyboardHookProc;

            // 实例化 KeyProcessing 对象，它负责处理复杂的按键组合、大小写、修饰键状态等
            _keyProcessing = new KeyProcessing();

            // 订阅 KeyProcessing 对象的事件，并将这些事件桥接到本类的公共事件上
            _keyProcessing.StringUp += _keyProcessing_StringUp;
            _keyProcessing.StringDown += _keyProcessing_StringDown;
        }

        /// <summary>
        /// 安装全局键盘钩子 (Hook)。
        /// </summary>
        public void Hook()
        {
            // 如果钩子句柄不为零，说明钩子已安装，直接返回
            if (_keyboardHookHandle != IntPtr.Zero)
                return;

            // 获取当前进程的模块句柄 (hMod)，这是安装全局钩子所必需的参数。
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                // 调用 NativeMethods 中封装的 SetWindowsHookEx API
                _keyboardHookHandle = NativeMethods.SetWindowsHookEx(
                    // HookType.WH_KEYBOARD_LL (通常值为 13)，表示低级键盘钩子
                    HookType.WH_KEYBOARD_LL,
                    // 使用已保留的委托实例
                    _procDelegate,
                    // 包含钩子过程的模块句柄
                    NativeMethods.GetModuleHandle(curModule.ModuleName),
                    // 线程ID。0 表示钩子将全局安装到系统中的所有线程
                    0);
            }

            // 检查安装是否成功
            if (_keyboardHookHandle == IntPtr.Zero)
            {
                // 如果失败，获取最近的 Win32 错误码
                var errorCode = Marshal.GetLastWin32Error();
                // 抛出 Win32Exception 异常，包含详细的错误描述
                throw new Win32Exception(errorCode);
            }

        }

        /// <summary>
        /// 卸载键盘钩子 (UnHook)。
        /// </summary>
        public void UnHook()
        {
            // 只有在钩子已安装时才执行卸载操作
            if (_keyboardHookHandle != IntPtr.Zero)
            {
                // 调用 NativeMethods 中封装的 UnhookWindowsHookEx API
                var result = NativeMethods.UnhookWindowsHookEx(_keyboardHookHandle);

                // 检查卸载是否失败
                if (result == false)
                {
                    // 卸载失败，获取并记录错误码（但这里未抛出异常，可能只是记录日志）
                    var errorCode = Marshal.GetLastWin32Error();
                    // 建议：可以抛出 Win32Exception 或记录详细日志
                }
                else
                {
                    // 卸载成功，清除钩子句柄
                    _keyboardHookHandle = IntPtr.Zero;
                }
            }
        }

        // =======================================================
        // KeyProcessing 事件转发器
        // =======================================================

        /// <summary>
        /// 处理 KeyProcessing 对象的 StringDown 事件，并转发给本类的订阅者。
        /// </summary>
        private void _keyProcessing_StringDown(object sender, StringDownEventArgs e)
        {
            // StringDown?.Invoke(...) 是线程安全的事件调用方式
            StringDown?.Invoke(this, e);
        }

        /// <summary>
        /// 处理 KeyProcessing 对象的 StringUp 事件，并转发给本类的订阅者。
        /// </summary>
        private void _keyProcessing_StringUp(object sender, StringDownEventArgs e)
        {
            StringUp?.Invoke(this, e);
        }

        // =======================================================
        // 钩子回调函数 (Hook Procedure)
        // =======================================================

        /// <summary>
        /// Windows 系统调用此函数来处理键盘事件。
        /// 使用 nint 保持与 NativeMethods.HookProc 委托签名的一致性。
        /// </summary>
        /// <param name="nCode">钩子代码，决定如何处理消息。</param>
        /// <param name="wParam">消息标识符（如 WM_KEYDOWN）。</param>
        /// <param name="lParam">指向 KBDLLHOOKSTRUCT 结构体的指针，包含按键细节。</param>
        /// <returns>下一个钩子过程返回的值。</returns>
        private nint KeyboardHookProc(int nCode, nint wParam, nint lParam)
        {
            // nCode < 0 时表示系统内部正在处理，必须调用 CallNextHookEx 并返回结果
            if (nCode >= 0)
            {
                var wParamInt = wParam.ToInt32();

                // 将非托管内存地址 (lParam) 转换为托管结构体 (KeyboardHookStruct)
                var myKeyboardHookStruct =
                    (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

                // 检查是否是“按键按下”事件 (WM_KEYDOWN 或 WM_SYSKEYDOWN)
                if ((StringDown != null) && (wParamInt == WM_KEYDOWN || wParamInt == WM_SYSKEYDOWN))
                {
                    // 再次检查 StringDown 是否有订阅者（冗余检查，但确保安全）
                    if (StringDown != null)
                    {
                        // 调用 KeyProcessing 对象来处理按键动作，true 表示按下
                        _keyProcessing.ProcessKeyAction((uint)myKeyboardHookStruct.VirtualKeyCode,
                            (uint)myKeyboardHookStruct.ScanCode, true);
                    }
                }

                // 检查是否是“按键抬起”事件 (WM_KEYUP 或 WM_SYSKEYUP)
                if ((StringUp != null) && (wParamInt == WM_KEYUP || wParamInt == WM_SYSKEYUP))
                {
                    if (StringUp != null)
                    {
                        // 调用 KeyProcessing 对象来处理按键动作，false 表示抬起
                        _keyProcessing.ProcessKeyAction((uint)myKeyboardHookStruct.VirtualKeyCode,
                            (uint)myKeyboardHookStruct.ScanCode, false);
                    }
                }
            }

            // 关键：将钩子信息传递给钩子链中的下一个钩子。
            // 注意：对于 WH_KEYBOARD_LL，通常推荐将 CallNextHookEx 的第一个参数设为 IntPtr.Zero。
            return NativeMethods.CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }
    }
}
