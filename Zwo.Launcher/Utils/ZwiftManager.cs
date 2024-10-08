﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace zlauncher.Zwift
{
    static class ZwiftManager
    {
        private static string _cachedZwiftKey = null;
        private static string _cachedInstallLocation = null;
        private static string _cachedVersion = null;
        private static string _cachedXmlVersion = null;

        public static void RunZwift(string zwiftInstallLocation)
        {
            string zwiftPath = Path.Combine(zwiftInstallLocation, "ZwiftLauncher.exe");

            Process.Start(new ProcessStartInfo
            {
                FileName = zwiftPath,
                UseShellExecute = true,
            });
        }

        public static void StopZwift()
        {
            Process[] processes = Process.GetProcessesByName("ZwiftLauncher");
            foreach (Process process in processes)
            {
                process.Kill(true);
                process.WaitForExit();
            }

            processes = Process.GetProcessesByName("ZwiftApp");
            foreach (Process process in processes)
            {
                process.Kill(true);
                process.WaitForExit();
            }
        }

        public static async Task<string> GetZwiftKeyAsync()
        {
            if (_cachedZwiftKey != null)
            {
                return _cachedZwiftKey;
            }

            StringBuilder output = new StringBuilder();

            await Task.Run(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c reg query HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall /s";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    output.AppendLine(process.StandardOutput.ReadLine());
                }
                process.WaitForExit();
            });

            string[] lines = output.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string zwiftKey = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("DisplayName") && line.Contains("Zwift"))
                {
                    for (int j = i - 1; j >= 0; --j)
                    {
                        if (lines[j].StartsWith("HKEY_LOCAL_MACHINE"))
                        {
                            zwiftKey = lines[j].Trim();
                            break;
                        }
                    }
                }
            }

            _cachedZwiftKey = zwiftKey;
            return _cachedZwiftKey;
        }

        public static async Task<string> GetInstallLocationAsync(string zwiftKey)
        {
            if (_cachedInstallLocation != null)
            {
                return _cachedInstallLocation;
            }

            StringBuilder output = new StringBuilder();

            await Task.Run(() =>
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c reg query \"{zwiftKey}\" /v InstallLocation";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    output.AppendLine(process.StandardOutput.ReadLine());
                }
                process.WaitForExit();
            });

            string result = output.ToString();
            int index = result.IndexOf("REG_SZ", StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                _cachedInstallLocation = result.Substring(index + 6).Trim();
                return _cachedInstallLocation;
            }

            return null;
        }

        public static string GetVersion(string installLocation)
        {
            if (_cachedVersion != null)
            {
                return _cachedVersion;
            }

            string filePath = Path.Combine(installLocation, "Zwift_ver_cur.xml");

            XmlDocument document = new XmlDocument();
            document.Load(filePath);

            XmlElement root = document.DocumentElement;
            _cachedVersion = root.GetAttribute("version");

            return _cachedVersion;
        }

        public static string GetXmlVersion(string installLocation)
        {
            if (_cachedXmlVersion != null)
            {
                return _cachedXmlVersion;
            }

            string filePath = Path.Combine(installLocation, "Zwift_ver_cur.xml");
            XmlDocument document = new XmlDocument();
            document.Load(filePath);

            _cachedXmlVersion = document.OuterXml;
            return _cachedXmlVersion;
        }
    }
}
