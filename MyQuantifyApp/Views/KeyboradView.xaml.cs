using Microsoft.Web.WebView2.Core;
using MyQuantifyApp.Database;
using MyQuantifyApp.Database.Repositories.Aggre;
using MyQuantifyApp.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MyQuantifyApp.Views
{
    public partial class KeyboradView : Page
    {
        private bool _isWebViewReady = false;
        private readonly SQLiteDataService _dbService;
        private bool _isInitialized = false;

        public KeyboradView()
        {
            InitializeComponent();
            Loaded += KeyboradView_Loaded;
            _dbService = new SQLiteDataService();
        }

        private async void KeyboradView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            await KeyboradWebView.EnsureCoreWebView2Async();

            string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Keyboard.html");
            string htmlUri = new Uri(htmlPath).AbsoluteUri;

            KeyboradWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;

            KeyboradWebView.NavigationCompleted += (s, ev) =>
            {
                _isWebViewReady = true;
                //Log.Information("🟢 WebView2 页面加载完成: {Uri}", htmlUri);
            };

            KeyboradWebView.WebMessageReceived += OnWebMessageReceived;

            KeyboradWebView.Source = new Uri(htmlUri);
        }


        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (KeyboradWebView?.CoreWebView2 == null) return;

            string? json = null;
            try
            {
                json = e.TryGetWebMessageAsString();
            }
            catch
            {
                try
                {
                    json = e.WebMessageAsJson;
                }
                catch (Exception ex)
                {
                    return;
                }
            }


            if (string.IsNullOrEmpty(json)) return;

            WebMessage? msg = null;
            try
            {
                msg = JsonSerializer.Deserialize<WebMessage>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ JSON 解析失败: {Json}", json);
                return;
            }

            if (msg == null) return;

            //Log.Debug("处理命令: {Cmd} _reqId={ReqId}", msg.Cmd, msg._reqId);

            await HandleWebMessageAsync(msg);
        }

        private async Task HandleWebMessageAsync(WebMessage msg)
        {
            if (!msg._reqId.HasValue)
            {
                //Log.Warning("⚠️ 收到没有 _reqId 的命令: {Cmd}", msg.Cmd);
                return;
            }

            int reqId = msg._reqId.Value;

            switch (msg.Cmd)
            {
                case "getKeyboardData":
                    {
                        string date = DateTime.Now.ToString("yyyy-MM-dd");

                        try
                        {
                            if (msg.Data.TryGetProperty("date", out var dateElement) && dateElement.ValueKind == JsonValueKind.String)
                            {
                                date = dateElement.GetString() ?? date;
                            }
                            //Log.Debug("🔧 从 JS 请求获取键盘数据: 日期={Date}", date);

                            var repo = new KeyAggregatesRepository(_dbService.ConnectionString);

                            Dictionary<string, int> keyData = repo.GetKeyCountsByDate(date);

                            var upperCaseKeyData = new Dictionary<string, int>();
                            foreach (var pair in keyData)
                            {
                                string upperKey = pair.Key.ToUpperInvariant();

                                if (upperCaseKeyData.ContainsKey(upperKey))
                                {
                                    upperCaseKeyData[upperKey] += pair.Value;
                                }
                                else
                                {
                                    upperCaseKeyData.Add(upperKey, pair.Value);
                                }
                            }

                            keyData = upperCaseKeyData;

                            var responsePayload = new { date, data = keyData };

                            await SendResponseAsync(reqId, responsePayload);
                            //Log.Information("✅ 返回 getKeyboardData 数据, _reqId={ReqId}, Count={Count}", reqId, keyData.Count);
                        }
                        catch (Exception ex)
                        {
                            //Log.Error(ex, "❌ 处理 getKeyboardData 失败: 日期={Date}", date);
                            await SendResponseAsync(reqId, new { date = date, data = new Dictionary<string, int>() });
                        }
                        break;
                    }

                case "queryHistory":
                    //Log.Warning("⚠️ KeyboradView 收到未实现的命令: {Cmd}", msg.Cmd);
                    break;

                default:
                    //Log.Warning("⚠️ 未知命令: {Cmd}", msg.Cmd);
                    break;
            }
        }

        private async Task SendResponseAsync(int? reqId, object data)
        {
            if (!reqId.HasValue) return;

            while (!_isWebViewReady || KeyboradWebView.CoreWebView2 == null)
                await Task.Delay(50);

            var response = new { _resId = reqId.Value, data };
            string json = JsonSerializer.Serialize(response);

            try
            {
                KeyboradWebView.CoreWebView2.PostWebMessageAsString(json);
                //Log.Information("🟢 已发送响应给 JS: {Json}", json);
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ 发送响应失败: {Json}", json);
            }
        }

        private async Task SendCommandAsync(string cmd, object data)
        {
            while (!_isWebViewReady || KeyboradWebView.CoreWebView2 == null)
                await Task.Delay(50);

            var msg = new { cmd, data };
            string json = JsonSerializer.Serialize(msg);

            try
            {
                KeyboradWebView.CoreWebView2.PostWebMessageAsString(json);
                //Log.Information("🟢 已发送命令给 JS: {Json}", json);
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "❌ 发送命令失败: {Json}", json);
            }
        }
    }
}