﻿using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Zwo.Launcher.Utils
{
    partial class ZofflineManager
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

        public static bool IsStarted { get; private set; } = false;
        private static Process _zofflineProcess;

        [GeneratedRegex(@"zoffline_(\d+\.\d+\.\d+)\.exe")]
        private static partial Regex VersionRegex();

        [GeneratedRegex(@"zoffline_(.*)$")]
        private static partial Regex ZofflineTagRegex();

        [GeneratedRegex(@"INFO:zoffline:Server version \d+\.\d+\.\d+ \(\d+\) is running\.")]
        private static partial Regex ZofflineStartLogRegex();

        private static void AppendTextToOutputBox(RichEditBox outputBox, string text)
        {
            outputBox.DispatcherQueue.TryEnqueue(() =>
            {
                outputBox.Document.GetText(TextGetOptions.None, out var sourceText);
                outputBox.IsReadOnly = false;
                outputBox.Document.SetText(TextSetOptions.None, $"{sourceText}{text}");
                outputBox.IsReadOnly = true;
            });
        }

        public static bool ShouldDownloadLatestVersion(out string latestLocalVersion)
        {
            var latestReleaseInfo = GetLatestReleaseInfo();
            (latestLocalVersion, var fileSize) = GetLocalLatestVersion();

            return
                string.IsNullOrEmpty(latestLocalVersion) ||
                CompareVersions(ParseZofflineVersion(latestReleaseInfo.TagName), latestLocalVersion) > 0 ||
                fileSize != latestReleaseInfo.Size;
        }

        public static void RunZoffline(string version, RichEditBox outputBox, Action onInfoDetected)
        {
            if (IsStarted) return;

            outputBox.DispatcherQueue.TryEnqueue(() =>
            {
                outputBox.IsReadOnly = false;
                outputBox.Document.SetText(TextSetOptions.None, string.Empty);
                outputBox.IsReadOnly = true;
            });

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

            _zofflineProcess = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true,
            };

            void dataReceivedEventHandler(object sender, DataReceivedEventArgs args)
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    AppendTextToOutputBox(outputBox, args.Data);

                    if (ZofflineStartLogRegex().IsMatch(args.Data))
                    {
                        onInfoDetected?.Invoke();
                    }
                }
            }

            _zofflineProcess.OutputDataReceived += dataReceivedEventHandler;
            _zofflineProcess.ErrorDataReceived += dataReceivedEventHandler;

            _zofflineProcess.Exited += (sender, args) =>
            {
                IsStarted = false;

                int exitCode = _zofflineProcess.ExitCode;
                AppendTextToOutputBox(outputBox, $"[zoffline 程序退出，代码为 {exitCode}]\n");

                _zofflineProcess.Dispose();
                _zofflineProcess = null;
            };

            _zofflineProcess.Start();
            _zofflineProcess.BeginOutputReadLine();
            _zofflineProcess.BeginErrorReadLine();
            IsStarted = true;

            return;
        }

        public static void StopZoffline()
        {
            if (!IsStarted || _zofflineProcess == null || _zofflineProcess.HasExited) return;

            string zofflineProcessName = _zofflineProcess.ProcessName;

            _zofflineProcess.Kill(true);

            Process[] processes = Process.GetProcessesByName(zofflineProcessName);
            foreach (Process process in processes)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        public static int CompareVersions(string version1, string version2)
        {
            Version v1 = new(version1);
            Version v2 = new(version2);

            return v1.CompareTo(v2);
        }

        public static (string, long) GetLocalLatestVersion()
        {
            string zofflineDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".zlauncher", "zoffline");
            var files = Directory.GetFiles(zofflineDirectory, "zoffline_*.exe");

            Regex versionRegex = VersionRegex();

            var latestFile = files
                .Select(file => new
                {
                    Version = versionRegex.Match(Path.GetFileName(file)).Groups[1].Value,
                    FilePath = file,
                })
                .Where(x => !string.IsNullOrEmpty(x.Version))
                .OrderByDescending(x => new Version(x.Version))
                .FirstOrDefault();

            if (latestFile == null)
            {
                return (null, 0);
            }

            long fileSize = new FileInfo(latestFile.FilePath).Length;
            return (latestFile.Version, fileSize);
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
                    FileInfo fileInfo = new(filePath);
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

            var httpClient = ProxyManager.GetHttpClient();

            ConfigurationManager.LoadConfiguration();
            var url = ConfigurationManager.Config.DownloadAcceleration.IsEnabled ?
                ConfigurationManager.Config.DownloadAcceleration.Mirror + releaseInfo.BrowserDownloadUrl
                : releaseInfo.BrowserDownloadUrl;

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            progressBar.IsIndeterminate = false;

            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) != 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes != -1)
                {
                    double progress = (double)totalRead / totalBytes * 100;
                    progressBar.Value = progress;
                }
            }
            progressBar.Value = 100;
        }

        public static string ParseZofflineVersion(string zofflineTagName)
        {
            var regex = ZofflineTagRegex();
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
            var httpClient = ProxyManager.GetHttpClient();
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

        public static async Task<List<ReleaseInfo>> GetReleaseInfosAsync()
        {
            if (_cachedReleaseInfos != null)
            {
                return _cachedReleaseInfos;
            }

            var url = "https://api.github.com/repos/zoffline/zwift-offline/releases";
            var httpClient = ProxyManager.GetHttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible)");

            var response = await httpClient.GetStringAsync(url);
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
