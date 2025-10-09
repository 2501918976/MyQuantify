using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Services.Other
{
    /// <summary>
    /// KBDLLHOOKSTRUCT 结构体在 C# 中的表示。
    /// 包含有关低级键盘输入事件的信息。
    /// 它是 SetWindowsHookEx 回调函数（HookProc）中 lParam 参数所指向的数据结构。
    /// </summary>
    /// <remarks>
    /// 原始文档参考：http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
    /// (注意：链接指向的是 CWPSTRUCT，但此处用于 KBDLLHOOKSTRUCT)
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)] // 指示 CLR 按照字段在结构体中的声明顺序，依次将其布局到非托管内存中，确保与 Windows API 结构体内存布局一致。
    internal struct KeyboardHookStruct
    {
        /// <summary>
        /// 指定一个虚拟键码（Virtual-Key Code，简称 VK Code）。
        /// 范围通常在 1 到 254 之间。它是 Windows 用来标识按键的标准方式（例如 VK_A，VK_RETURN）。
        /// </summary>
        public int VirtualKeyCode;

        /// <summary>
        /// 指定该键的硬件扫描码（Scan Code）。
        /// 这是键盘硬件报告的原始代码，不依赖于键盘布局。
        /// </summary>
        public int ScanCode;

        /// <summary>
        /// 指定一组标志，包含：
        /// 扩展键标志 (LLKHF_EXTENDED)、事件注入标志 (LLKHF_INJECTED)、上下文代码 (LLKHF_ALTDOWN)、和转换状态标志 (LLKHF_UP/DOWN)。
        /// 用于区分左/右 Shift、Alt 键是否按下，以及消息是按下还是抬起。
        /// </summary>
        public int Flags;

        /// <summary>
        /// 指定此消息的时间戳（毫秒级）。
        /// </summary>
        public int Time;

        /// <summary>
        /// 指定与此消息关联的额外信息。
        /// 通常为零，除非消息是由 SendInput API 注入的。
        /// </summary>
        public int ExtraInfo;
    }
}
