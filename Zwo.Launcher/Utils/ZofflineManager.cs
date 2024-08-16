using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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
            public long Size { get; set; }
        }

        private static List<ReleaseInfo> _cachedReleaseInfos = null;
        private static ReleaseInfo _cachedLatestReleaseInfo = null;

        public static async Task DownloadZofflineAsync(ReleaseInfo releaseInfo, ProgressBar progressBar)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var downloadDirectory = Path.Combine(basePath, ".zlauncher", "zoffline");

            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            var fileName = Path.GetFileName(releaseInfo.BrowserDownloadUrl);
            var filePath = Path.Combine(downloadDirectory, fileName);

            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, releaseInfo.BrowserDownloadUrl);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            progressBar.DispatcherQueue.TryEnqueue(() => progressBar.IsIndeterminate = false);

            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) != 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes != -1 && progressBar != null)
                {
                    double progress = (double)totalRead / totalBytes * 100;
                    progressBar.DispatcherQueue.TryEnqueue(() =>
                    {
                        progressBar.Value = progress;
                    });
                }
            }

            if (progressBar != null)
            {
                progressBar.DispatcherQueue.TryEnqueue(() =>
                {
                    progressBar.Value = 100;
                });
            }
        }

        public static string ParseZofflineVersion(string zofflineTagName)
        {
            var regex = new Regex(@"zoffline_(.*)$");
            var match = regex.Match(zofflineTagName);
            return match.Success ? match.Groups[1].Value : null;
        }

        public static ReleaseInfo GetLatestReleaseInfo()
        {
            if (_cachedLatestReleaseInfo != null)
            {
                return _cachedLatestReleaseInfo;
            }

            var url = "https://api.github.com/repos/zoffline/zwift-offline/releases/latest";
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible)");

            var response = httpClient.GetStreamAsync(url).Result;
            var release = JsonSerializer.Deserialize<JsonElement>(response);

            var releaseInfo = new ReleaseInfo
            {
                Id = release.GetProperty("id").GetInt32(),
                TagName = release.GetProperty("tag_name").GetString(),
                HtmlUrl = release.GetProperty("html_url").GetString(),
                PublishedAt = release.GetProperty("published_at").GetDateTime(),
                BrowserDownloadUrl = release.GetProperty("assets")[0].GetProperty("browser_download_url").GetString(),
                Size = release.GetProperty("assets")[0].GetProperty("size").GetInt64()
            };

            _cachedLatestReleaseInfo = releaseInfo;
            return _cachedLatestReleaseInfo;
        }

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
                    BrowserDownloadUrl = release.GetProperty("assets")[0].GetProperty("browser_download_url").GetString(),
                Size = release.GetProperty("assets")[0].GetProperty("size").GetInt64()
                });
            }

            _cachedReleaseInfos = releaseInfos;
            return _cachedReleaseInfos;
        }
    }
}
