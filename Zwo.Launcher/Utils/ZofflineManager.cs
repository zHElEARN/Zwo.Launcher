using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Zwo.Launcher.Utils
{
    class ZofflineManager
    {
        public class ReleaseInfo
        {
            public int Id { get; set; }
            public string TagName { get; set; }
            public string Name { get; set; }
            public string HtmlUrl { get; set; }
            public DateTime PublishedAt { get; set; }
            public string BrowserDownloadUrl { get; set; }
        }

        private static List<ReleaseInfo> _cachedReleaseInfos = null;

        public static List<ReleaseInfo> GetReleaseInfos()
        {
            if (_cachedReleaseInfos != null)
            {
                return _cachedReleaseInfos;
            }

            var url = "https://api.github.com/repos/zoffline/zwift-offline/releases";
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible)");

            var response = httpClient.GetStringAsync(url).Result;
            var releases = JsonSerializer.Deserialize<List<JsonElement>>(response);
            var releaseInfos = new List<ReleaseInfo>();

            foreach (var release in releases)
            {
                releaseInfos.Add(new ReleaseInfo
                {
                    Id = release.GetProperty("id").GetInt32(),
                    TagName = release.GetProperty("tag_name").GetString(),
                    Name = release.GetProperty("name").GetString(),
                    HtmlUrl = release.GetProperty("html_url").GetString(),
                    PublishedAt = release.GetProperty("published_at").GetDateTime(),
                    BrowserDownloadUrl = release.GetProperty("assets")[0].GetProperty("browser_download_url").GetString()
                });
            }

            _cachedReleaseInfos = releaseInfos;
            return _cachedReleaseInfos;
        }
    }
}
