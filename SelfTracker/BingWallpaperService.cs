using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SelfTracker
{
    public class BingWallpaperService
    {
        private const string BingApiUrl = "https://cn.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1";

        public static async Task<string> GetBingWallpaperUrl()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string jsonString = await client.GetStringAsync(BingApiUrl);

                using JsonDocument doc = JsonDocument.Parse(jsonString);
                // 提取 images[0].url 字段
                string relativeUrl = doc.RootElement.GetProperty("images")[0].GetProperty("url").GetString();

                return "https://www.bing.com" + relativeUrl;
            }
            catch
            {
                return null; // 联网失败则返回空
            }
        }
    }
}
