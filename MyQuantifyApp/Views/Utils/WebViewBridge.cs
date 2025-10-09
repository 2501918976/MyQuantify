using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyQuantifyApp.Views.Utils
{
    public class WebViewBridge
    {
        private readonly WebView2 _webView;
        private readonly Dictionary<string, Func<JsonElement, object?>> _handlers = new();

        public WebViewBridge(WebView2 webView)
        {
            _webView = webView;
            _webView.WebMessageReceived += OnMessageReceived;
        }

        public void On(string cmd, Func<JsonElement, object?> handler)
        {
            _handlers[cmd] = handler;
        }

        private void OnMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var json = e.TryGetWebMessageAsString();
            var msg = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json!);

            if (msg == null) return;

            // 异步请求带 _reqId
            if (msg.TryGetValue("_reqId", out var reqIdElement))
            {
                var cmd = msg["cmd"].GetString()!;
                var data = msg.GetValueOrDefault("data");
                if (_handlers.TryGetValue(cmd, out var handler))
                {
                    var result = handler(data);
                    var response = new
                    {
                        _resId = reqIdElement.GetInt32(),
                        data = result
                    };
                    _webView.CoreWebView2.PostWebMessageAsString(JsonSerializer.Serialize(response));
                }
                return;
            }

            // 普通消息
            if (msg.TryGetValue("cmd", out var c))
            {
                var cmd = c.GetString()!;
                if (_handlers.TryGetValue(cmd, out var handler))
                {
                    handler(msg.GetValueOrDefault("data"));
                }
            }
        }

        public void Send(string cmd, object? data = null)
        {
            var msg = new { cmd, data };
            _webView.CoreWebView2.PostWebMessageAsString(JsonSerializer.Serialize(msg));
        }
    }
}
