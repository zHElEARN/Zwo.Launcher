using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace zlauncher.Zoffline
{
    class ZofflineManager
    {
        public struct ZofflineInfo
        {
            public string Name { get; set; }
            public string DownloadUrl { get; set; }
            public long Size { get; set; }
        }

        public static async Task<ZofflineInfo> GetLatestInfo()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = "https://api.github.com/repos/zoffline/zwift-offline/releases/latest";
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GrandCircusClient/1.0)");

                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();

                    using (JsonDocument jsonDoc = JsonDocument.Parse(responseBody))
                    {
                        JsonElement root = jsonDoc.RootElement;
                        root.TryGetProperty("assets", out JsonElement assets);
                        JsonElement firstAsset = assets[0];
                        string name = firstAsset.GetProperty("name").GetString();
                        string downloadUrl = firstAsset.GetProperty("browser_download_url").GetString();
                        long size = firstAsset.GetProperty("size").GetInt64();

                        return new ZofflineInfo
                        {
                            Name = name,
                            DownloadUrl = downloadUrl,
                            Size = size
                        };
                    }
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine(ex.Message);
                    return default;
                }
            }
        }
    }
}
