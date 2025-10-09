using System;

namespace MyQuantifyApp.Services.Other
{
    internal class StringDownEventArgs : EventArgs
    {
        /// <summary>
        /// 构造函数，用于初始化按键事件参数。
        /// </summary>
        /// <param name="isChar">指示是否为字符键。</param>
        /// <param name="value">按键的字符串值。</param>
        /// <param name="vCode">虚拟键码（Virtual Key Code）。</param>
        public StringDownEventArgs(bool isChar, string value, uint vCode)
        {
            IsChar = isChar;
            Value = value;

            // 解决 CS1061 错误：将传入的 vCode 赋值给所需的 VirtualKeyCode 属性。
            VirtualKeyCode = vCode;
        }

        /// <summary>
        /// 获取一个值，指示按下的键是否代表一个字符。
        /// </summary>
        public bool IsChar { get; }

        /// <summary>
        /// 获取按下的键的字符串值。
        /// </summary>
        public string Value { get; }

        // 解决了 CS1061 错误：将原来的 VCode 属性更名为 VirtualKeyCode。
        /// <summary>
        /// 获取按键的虚拟键码 (VK Code)。
        /// </summary>
        public uint VirtualKeyCode { get; }

        // 解决了 CS1061 错误：将原来的 IsHandled 属性更名为 Result。
        // Result 属性通常用于在事件处理程序中设置是否阻止事件的进一步传播或表示操作结果。
        /// <summary>
        /// 获取或设置一个值，表示事件处理的结果或是否已处理。
        /// </summary>
        public bool Result { get; set; }
    }
}
