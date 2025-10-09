using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyQuantifyApp.Views.Utils
{
    /// <summary>
    /// JS → C# 消息格式
    /// </summary>
    public class WebMessage
    {
        [JsonPropertyName("cmd")]
        public string Cmd { get; set; } = "";

        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }

        [JsonPropertyName("_reqId")]
        public int? _reqId { get; set; }
    }
}
