using System;
using System.Runtime.InteropServices;

namespace SelfTracker.DataCollectors
{
    /// <summary>
    /// 挂机/空闲时间收集器，用于监测用户是否离开电脑
    /// </summary>
    public class AFKCollector
    {
        /// <summary>
        /// 获取当前系统总的空闲时间（即没有任何输入操作持续的时间）
        /// </summary>
        public TimeSpan IdleTime => GetIdleTime();

        /// <summary>
        /// 判断用户是否处于挂机状态
        /// </summary>
        /// <param name="threshold">判定的阈值（例如：超过5分钟无操作则视为AFK）</param>
        /// <returns>如果空闲时间大于或等于阈值，返回 true</returns>
        public bool IsAFK(TimeSpan threshold) => IdleTime >= threshold;

        /// <summary>
        /// 核心方法：通过 Win32 API 计算自上次输入以来的毫秒数
        /// </summary>
        private TimeSpan GetIdleTime()
        {
            // 初始化 LASTINPUTINFO 结构体，用于接收系统返回的信息
            LASTINPUTINFO info = new LASTINPUTINFO();
            // 必须设置 cbSize 成员，告知系统该结构体的大小，否则调用会失败
            info.cbSize = (uint)Marshal.SizeOf(info);

            // 调用 Windows User32.dll 中的 API
            // 如果调用失败（返回 false），则假定空闲时间为 0
            if (!GetLastInputInfo(ref info))
                return TimeSpan.Zero;

            // Environment.TickCount 表示系统启动后经过的时间（毫秒数）
            // 它是 int 类型，运行约 24.9 天后会溢出变成负数
            // 使用 unchecked 强制转换为 uint 可以确保减法运算在溢出时依然得到正确的差值
            uint systemTicks = unchecked((uint)Environment.TickCount);

            // dwTime 是最后一次输入事件发生时的系统 TickCount
            // 当前 Tick 减去 最后输入 Tick = 用户已经空闲的毫秒数
            uint idleTicks = systemTicks - info.dwTime;

            return TimeSpan.FromMilliseconds(idleTicks);
        }

        /// <summary>
        /// LASTINPUTINFO 结构体，对应 Win32 API 中的同名结构
        /// [StructLayout] 确保结构体成员在内存中的顺序与系统要求一致
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize; // 结构体本身的大小（字节）
            public uint dwTime; // 最后一次输入操作的时间戳（毫秒级）
        }

        /// <summary>
        /// 导入 user32.dll 中的原生函数
        /// 该函数会获取全系统范围内最后一次输入（键盘、鼠标、触摸等）的时间
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
    }
}