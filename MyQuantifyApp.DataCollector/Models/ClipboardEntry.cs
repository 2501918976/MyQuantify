using System;
using System.ComponentModel.DataAnnotations;

namespace MyQuantifyApp.DataCollector.Models
{
    /// <summary>
    /// 剪贴板记录模型，用于记录复制操作的时间和内容。
    /// </summary>
    public class ClipboardEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime CopyTime { get; set; }

        // 使用 string 类型对应数据库的 TEXT 类型，可以存储较长的文本
        [Required]
        public string Content { get; set; }

        // 内容长度，用于快速筛选和分析
        public int ContentLength { get; set; }

        // 内容哈希值，用于检测重复内容
        [MaxLength(64)]
        public string ContentHash { get; set; }
    }
}
