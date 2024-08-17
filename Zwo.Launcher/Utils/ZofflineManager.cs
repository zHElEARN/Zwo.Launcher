using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            public bool IsExistingLocally { get; set; } = false;
        }

        private static List<ReleaseInfo> _cachedReleaseInfos = null;
        private static ReleaseInfo _cachedLatestReleaseInfo = null;

        public static async Task RunZofflineAsync(string version, RichEditBox outputBox)
        {
            string zofflineDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".zlauncher", "zoffline");
            string exePath = Path.Combine(zofflineDirectory, $"zoffline_{version}.exe");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true,
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    outputBox.DispatcherQueue.TryEnqueue(() =>
                    {
                        outputBox.IsReadOnly = false;
                        outputBox.Document.Selection.SetRange(outputBox.Document.Selection.EndPosition, outputBox.Document.Selection.EndPosition);
                        outputBox.Document.Selection.Text = $"{args.Data}\n";
                        outputBox.IsReadOnly = true;
                    });
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    outputBox.DispatcherQueue.TryEnqueue(() =>
                    {
                        outputBox.IsReadOnly = false;
                        outputBox.Document.Selection.SetRange(outputBox.Document.Selection.EndPosition, outputBox.Document.Selection.EndPosition);
                        outputBox.Document.Selection.Text = $"{args.Data}\n";
                        outputBox.IsReadOnly = true;
                    });
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() => process.WaitForExit());
        }

        public static int CompareVersions(string version1, string version2)
        {
            Version v1 = new Version(version1);
            Version v2 = new Version(version2);

            return v1.CompareTo(v2);
        }

        public static string GetLocalLatestVersion()
        {
            string zofflineDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".zlauncher", "zoffline");
            var files = Directory.GetFiles(zofflineDirectory, "zoffline_*.exe");

            Regex versionRegex = new Regex(@"zoffline_(\d+\.\d+\.\d+)\.exe");

            var latestFile = files
                .Select(file => versionRegex.Match(Path.GetFileName(file)).Groups[1].Value)
                .Where(version => !string.IsNullOrEmpty(version))
                .OrderByDescending(version => new Version(version))
                .FirstOrDefault();

            return latestFile;
        }

        public static void MarkExistingZofflineFiles(List<ReleaseInfo> releaseInfos)
        {
            string zofflineDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".zlauncher", "zoffline");

            if (!Directory.Exists(zofflineDirectory))
            {
                return;
            }

            var existingFiles = Directory.GetFiles(zofflineDirectory, "*.exe");

            foreach (var releaseInfo in releaseInfos)
            {
                string expectedFileName = $"{releaseInfo.TagName}.exe";
                string filePath = existingFiles.FirstOrDefault(f => Path.GetFileName(f) == expectedFileName);

                if (filePath != null)
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length == releaseInfo.Size)
                    {
                        releaseInfo.IsExistingLocally = true;
                    }
                }
            }
        }

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
