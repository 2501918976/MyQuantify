using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace SelfTracker.Setting
{
    public class AppSettings
    {
        #region 设置参数

        /// <summary>
        /// 数据入库频率（秒），范围：20-600
        /// </summary>
        public int LogIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// AFK 判断超时时间（秒），范围：15-1200
        /// </summary>
        public int AFKTimeoutSeconds { get; set; } = 180;


        /// <summary>
        /// 主题颜色
        /// </summary>
        public string ThemeColor { get; set; } = "#4E73DF";

        /// <summary>
        /// 字体颜色
        /// </summary>
        public string TextColor { get; set; } = "#2C3E50";

        /// <summary>
        /// 背景透明度，范围：0.3-1.0
        /// </summary>
        public double BackgroundOpacity { get; set; } = 0.95;

        /// <summary>
        /// 背景模式：None, Bing, Custom
        /// </summary>
        public string BackgroundMode { get; set; } = "None";

        /// <summary>
        /// 自定义背景图片路径
        /// </summary>
        public string CustomBackgroundPath { get; set; }

        /// <summary>
        /// 是否开机自动启动
        /// </summary>
        public bool AutoStart { get; set; } = false;

        #endregion

        #region 保存和加载

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void Save()
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化为 JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);

                    // 验证并修正配置值
                    settings.ValidateAndFix();

                    return settings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
            }

            // 返回默认配置
            return new AppSettings();
        }

        /// <summary>
        /// 验证并修正配置值
        /// </summary>
        private void ValidateAndFix()
        {
            // 验证数据入库频率
            if (LogIntervalSeconds < 20)
                LogIntervalSeconds = 20;
            else if (LogIntervalSeconds > 600)
                LogIntervalSeconds = 600;

            // 验证 AFK 超时时间
            if (AFKTimeoutSeconds < 15)
                AFKTimeoutSeconds = 15;
            else if (AFKTimeoutSeconds > 1200)
                AFKTimeoutSeconds = 1200;

            // 验证透明度
            if (BackgroundOpacity < 0.3)
                BackgroundOpacity = 0.3;
            else if (BackgroundOpacity > 1.0)
                BackgroundOpacity = 1.0;

            // 验证背景模式
            if (BackgroundMode != "None" && BackgroundMode != "Bing" && BackgroundMode != "Custom")
                BackgroundMode = "None";

            // 验证主题颜色
            if (string.IsNullOrWhiteSpace(ThemeColor))
                ThemeColor = "#4E73DF";
        }
        #endregion
    }
}
