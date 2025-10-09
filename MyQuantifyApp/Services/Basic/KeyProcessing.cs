using MyQuantifyApp.Services.Enems;
using MyQuantifyApp.Services.Native;
using MyQuantifyApp.Services.Other;
using System; // 基础类型，如 IntPtr, EventArgs
using System.Collections; // 引入 ArrayList，用于存储死键序列
using System.Text; // 引入 StringBuilder，用于接收 Windows API 转换后的字符
using System.Windows.Input; // WPF 的 Keyboard

namespace MyQuantifyApp.Service.Services
{
    /// <summary>
    /// 负责将键盘钩子捕获的原始虚拟键码（vkcode）转换为字符或字符串，
    /// 并处理死键（Dead Key）和修饰键（Modifier Key）状态。
    /// </summary>
    internal class KeyProcessing
    {
        // =======================================================
        // 死键 (Dead Key) 状态和缓冲
        // =======================================================
        private readonly ArrayList _deadKeys = new ArrayList(); // 存储死键事件及其上下文（vkcode, 扫描码, 键盘状态等）的序列。
        private bool _deadKeyOver; // 标志：死键序列是否结束（即死键后按下了第二个字符键）。
        private bool _lastWasDeadKey; // 标志：上一个处理的按键是否是死键。

        // =======================================================
        // 公共事件：通知外部已处理的字符/字符串结果
        // =======================================================
        public event EventHandler<StringDownEventArgs> StringDown; // 字符生成事件（按下）。
        public event EventHandler<StringDownEventArgs> StringUp; // 字符生成事件（抬起）。

        // =======================================================
        // 私有方法：最终的按键转换和事件触发
        // =======================================================

        /// <summary>
        /// 执行最终的按键转换和事件触发。
        /// </summary>
        /// <param name="vkcode">虚拟键码。</param>
        /// <param name="nScanCode">硬件扫描码。</param>
        /// <param name="isDown">是否是按下事件。</param>
        /// <param name="kbstate">捕获的键盘状态数组（256 字节）。</param>
        private void OnKeyActionFurtherProcessing2(uint vkcode, uint nScanCode, bool isDown, byte[] kbstate)
        {
            // 默认结果：如果无法转换为字符，则使用虚拟键码对应的枚举名称（如 "LControlKey", "D1"）。
            var result = ((System.Windows.Forms.Keys)vkcode).ToString();

            // 1. 判断是否为可打印键 且 CTRL 键未按下
            if (IsPrintableKey(vkcode) && !IsCtrlPressed())
            {
                var szKey = new StringBuilder(2); // 准备一个 StringBuilder 来接收转换后的字符。

                // 调用 Windows API ToAscii (或 ToUnicode/ToAsciiEx)，将虚拟键码转换为字符。
                // 转换结果 nConvOld 可能为 0, 1（单个字符）, 2（两个字符，不常见）, 或负值（死键）。
                var nConvOld = (uint)NativeMethods.ToAscii(vkcode, nScanCode, kbstate, szKey, 0);

                _deadKeyOver = false; // 如果按下了普通键，则重置死键结束标志。

                // 检查 ToAscii 是否成功转换出字符
                if (nConvOld > 0 && szKey.Length > 0)
                    result = szKey.ToString().Substring(0, 1); // 取转换结果的第一个字符作为结果。
            }

            // 2. 触发事件
            if (isDown)
            {
                // 修正: 使用 'this' 作为事件发送者
                StringDown?.Invoke(this, new StringDownEventArgs(result.Length == 1, result, vkcode));
            }
            else
            {
                // 修正: 使用 'this' 作为事件发送者
                StringUp?.Invoke(this, new StringDownEventArgs(result.Length == 1, result, vkcode));
            }
        }

        // =======================================================
        // 内部方法：死键处理和调度
        // =======================================================

        /// <summary>
        /// 处理从键盘钩子回调函数中收到的原始按键事件。
        /// 这是死键逻辑的核心入口点。
        /// </summary>
        /// <param name="vkcode">虚拟键码。</param>
        /// <param name="nScanCode">硬件扫描码。</param>
        /// <param name="isDown">是否是按下事件。</param>
        internal void ProcessKeyAction(uint vkcode, uint nScanCode, bool isDown)
        {
            // 1. 如果当前按键是死键（例如重音符号键）
            if (IsDeadKey(vkcode))
            {
                _lastWasDeadKey = true; // 标记上一个按键是死键。
                var oldKbstate = MyGetKeyboardState(); // 获取当前键盘状态。
                // 将死键事件及其上下文（键码、扫描码、键盘状态）存入死键列表，等待后续字符组合。
                _deadKeys.Add(new object[] { vkcode, nScanCode, isDown, oldKbstate });
                return; // 结束处理，等待下一个键。
            }

            // 2. 如果上一个按键是死键，而当前按键不是死键（即触发了死键组合）
            if (_lastWasDeadKey)
            {
                var oldKbstate = MyGetKeyboardState(); // 获取当前键盘状态。
                _deadKeyOver = true; // 标记死键序列结束。
                _lastWasDeadKey = false; // 重置死键标志。
                // 将当前按键事件及其上下文存入死键列表（作为死键序列的第二个字符）。
                _deadKeys.Add(new object[] { vkcode, nScanCode, isDown, oldKbstate });
                // 注意：这里没有 return，代码将继续执行到下面的 _deadKeyOver 逻辑块。
            }

            // 3. 处理已完成的死键序列
            if (_deadKeyOver)
            {
                // 遍历并处理死键列表中存储的所有按键（包括死键本身和随后的字符键）
                foreach (var obj in _deadKeys)
                {
                    var objArray = (object[])obj;

                    // 对列表中的每个键调用最终处理方法
                    OnKeyActionFurtherProcessing2((uint)objArray[0], (uint)objArray[1], (bool)objArray[2],
                        (byte[])objArray[3]);

                    // **死键清除逻辑**：如果列表中的键是死键，需要调用 ToAscii 清除死键状态。
                    // Windows 的 ToAscii/ToUnicode API 在处理死键时，会将状态保存在键盘布局文件中。
                    // 再次调用 ToAscii (不传入实际按下的键) 会清除或转换悬挂的死键状态。
                    if (IsDeadKey((uint)objArray[0]))
                        NativeMethods.ToAscii(vkcode, nScanCode, (byte[])objArray[3], new StringBuilder(2), 0);
                }

                _deadKeys.Clear(); // 清空死键列表，完成一个死键组合的处理。
            }

            // 4. 处理普通按键
            // 如果不是死键，且不是死键序列的结束部分，则执行普通按键的转换。
            var kbstate = MyGetKeyboardState(); // 获取最新键盘状态
            OnKeyActionFurtherProcessing2(vkcode, nScanCode, isDown, kbstate);
        }

        // =======================================================
        // 辅助方法：状态和键类型检查
        // =======================================================

        /// <summary>
        /// 获取当前键盘状态的快照。
        /// 状态数组的每个字节对应一个虚拟键的状态（按下/弹起，开关/锁定）。
        /// </summary>
        /// <returns>包含 256 字节的键盘状态数组。</returns>
        private static byte[] MyGetKeyboardState()
        {
            var result = new byte[256];
            for (var i = 0; i < result.Length; i++)
            {
                // GetKeyState 检查指定虚拟键码的状态。
                result[i] = (byte)NativeMethods.GetKeyState(i);
            }

            return result;
        }

        /// <summary>
        /// 检查虚拟键码是否为可打印字符键。
        /// 0x20 是空格键 (VK_SPACE)。通常认为大于或等于空格键的键是可打印键。
        /// </summary>
        private bool IsPrintableKey(uint vkCode)
        {
            return vkCode >= 0x20;
        }

        /// <summary>
        /// 检查虚拟键码是否为死键（Dead Key）。
        /// 死键是用于生成重音字符（如 'é', 'ñ'）的键。
        /// </summary>
        private static bool IsDeadKey(uint vkCode)
        {
            // MapVirtualKey 的 MAPVK_VK_TO_CHAR 模式用于将虚拟键码映射到字符代码。
            // 如果结果的最高位 (0x80000000) 被设置，则表示该键是死键。
            return (NativeMethods.MapVirtualKey(vkCode, MapVirtualKeyMapTypes.MAPVK_VK_TO_CHAR) & 0x80000000) != 0;
        }

        /// <summary>
        /// 检查 Control (Ctrl) 修饰键是否处于按下状态。
        /// 使用 System.Windows.Input.Keyboard 类提供的方法。
        /// </summary>
        private bool IsCtrlPressed()
        {
            return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        }
    }
}
