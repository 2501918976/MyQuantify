using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MyQuantifyApp.DataCollector.Utilities
{
    /// <summary>
    /// 用于计算字符串内容的哈希值的辅助类。
    /// </summary>
    public static class HashHelper
    {
        /// <summary>
        /// 计算给定字符串的 SHA256 哈希值。
        /// </summary>
        /// <param name="content">要计算哈希值的字符串。</param>
        /// <returns>SHA256 哈希值的十六进制字符串表示。</returns>
        public static string CalculateSha256(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(content);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // 转换为十六进制字符串
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
