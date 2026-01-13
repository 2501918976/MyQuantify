namespace SelfTracker.Setting
{
    public class AppSettings
    {
        // --- 采集频率相关 ---

        /// <summary>
        /// 核心轮询频率（毫秒），默认 1000ms (1秒)
        /// 决定了检测窗口切换和 AFK 的灵敏度
        /// </summary>
        public int CoreTickIntervalMs { get; set; } = 1000;

        /// <summary>
        /// 生产力数据（打字/复制）入库频率（秒）
        /// 对应你代码中的 FlushIntervalSeconds
        /// </summary>
        public int LogIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 用户空闲（AFK）判定阈值（秒）
        /// 超过此时间没有操作则认为进入 AFK 状态
        /// </summary>
        public int AFKTimeoutSeconds { get; set; } = 300; // 默认 5 分钟

        // --- 系统相关 ---

        public bool AutoStart { get; set; } = false;

        public AppSettings Clone()
        {
            return new AppSettings
            {
                CoreTickIntervalMs = CoreTickIntervalMs,
                LogIntervalSeconds = LogIntervalSeconds,
                AFKTimeoutSeconds = AFKTimeoutSeconds,
                AutoStart = AutoStart
            };
        }
    }
}