using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.DataCollector.Models
{
    /// <summary>
    /// 记录用户在前台使用每一个窗口的会话信息，包括活动、状态和性能指标。
    /// </summary>
    public class ActivitySession // 类名已更改为 ActivitySession
    {
        [Key] // 数据库主键
        public int Id { get; set; }

        // --- 核心窗口信息 ---
        [Required] // 确保字段不为空
        [MaxLength(512)] // 限制最大长度
        public string WindowTitle { get; set; }

        [Required]
        [MaxLength(128)]
        public string ProcessName { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        // 允许为空，直到窗口切换或关闭时才记录
        public DateTime? EndTime { get; set; }

        // [NotMapped] 属性保持不变，用于代码内部计算
        [NotMapped]
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;


        // --- 停留时长与上下文 ---

        /// <summary>
        /// 【停留时长】以秒为单位。窗口切换到后台时计算并写入数据库。
        /// </summary>
        public int? DurationSeconds { get; set; }

        /// <summary>
        /// 【前后上下文】记录该窗口被激活之前的前一个 ActivitySession 的 ID。
        /// 用于分析应用切换的顺序。
        /// </summary>
        public int? PreviousSessionId { get; set; } // 属性名已更新

        [ForeignKey("PreviousSessionId")]
        public virtual ActivitySession PreviousSession { get; set; } // 导航属性名和类型已更新


        // --- 窗口状态和活动 ---

        /// <summary>
        /// 【屏幕是否全屏】记录窗口是否占据了整个屏幕，通常用于识别游戏或视频播放。
        /// </summary>
        public bool IsFullscreen { get; set; }

        /// <summary>
        /// 【音频活动】该窗口在前台时，是否有音频播放（需要调用音频 API 检测）。
        /// </summary>
        public bool IsAudioPlaying { get; set; }


        // --- 新增属性：性能指标 ---

        /// <summary>
        /// 【平均 CPU 使用率】该进程在会话期间的平均 CPU 使用百分比（百分比值，如 5.5 代表 5.5%）。
        /// </summary>
        public float? AvgCpuUsagePercent { get; set; }

        /// <summary>
        /// 【平均内存占用】该进程在会话期间的平均工作集内存（MB）。
        /// </summary>
        public long? AvgMemoryUsageMB { get; set; }

        /// <summary>
        /// 【平均网络数据速率】该进程在会话期间平均每秒传输的数据量（KB/s）。
        /// </summary>
        public float? AvgNetworkDataKBps { get; set; }
    }
}
