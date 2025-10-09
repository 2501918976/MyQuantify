using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Services.Enems
{
    public enum MapVirtualKeyMapTypes : uint // 使用 uint 作为底层数据类型
    {
        /// <summary>
        /// 映射类型 0x00：将虚拟键码转换为扫描码。
        /// uCode 是一个虚拟键码 (VK Code)，将被转换为对应的硬件扫描码 (Scan Code)。
        /// 如果该虚拟键码不区分左手键和右手键（例如标准的 SHIFT），则返回左手键的扫描码。
        /// 如果没有对应的转换，函数返回 0。
        /// </summary>
        MAPVK_VK_TO_VSC = 0x00,

        /// <summary>
        /// 映射类型 0x01：将扫描码转换为不区分左右手的虚拟键码。
        /// uCode 是一个扫描码 (Scan Code)，将被转换为一个不区分左/右手按键的虚拟键码。
        /// 如果没有对应的转换，函数返回 0。
        /// </summary>
        MAPVK_VSC_TO_VK = 0x01,

        /// <summary>
        /// 映射类型 0x02：将虚拟键码转换为未移位的字符值。
        /// uCode 是一个虚拟键码 (VK Code)，返回值（低位字）是未按下 Shift 或 Alt 等修饰键时对应的字符值。
        /// **关键作用：** 如果该键是 **死键 (Dead Key)**（用于输入重音符号的键，如在某些键盘布局中的 '`' 或 '^'），
        /// 返回值的最高位 (0x80000000) 将被设置。这是判断死键的主要方式。
        /// 如果没有对应的转换，函数返回 0。
        /// </summary>
        MAPVK_VK_TO_CHAR = 0x02,

        /// <summary>
        /// 映射类型 0x03：将扫描码转换为区分左右手的虚拟键码。
        /// uCode 是一个扫描码 (Scan Code)，将被转换为一个**区分左/右手按键**（例如 VK_LSHIFT, VK_RSHIFT）的虚拟键码。
        /// 如果没有对应的转换，函数返回 0。
        /// </summary>
        MAPVK_VSC_TO_VK_EX = 0x03,

        /// <summary>
        /// 映射类型 0x04：将区分左右手的虚拟键码转换为扫描码。
        /// 当前没有官方文档说明，但在某些 Windows 版本和应用中可能存在特定用途。
        /// </summary>
        MAPVK_VK_TO_VSC_EX = 0x04
    }
}
