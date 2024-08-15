using System;
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
        private static Tuple<bool, bool> _cachedIsZwiftInstalled = new Tuple<bool, bool>(false, false);
        private static string _cachedZwiftKey = null;

        private static Tuple<bool, string> _cachedInstallLocation = new Tuple<bool, string>(false, null);
        private static Tuple<bool, string> _cachedVersion = new Tuple<bool, string>(false, null);
        private static Tuple<bool, string> _cachedXmlVersion = new Tuple<bool, string>(false, null);

        /// <summary>
        /// 检测系统是否安装Zwift
        /// </summary>
        /// <returns>是否安装Zwift</returns>
        public static async Task<bool> IsZwiftInstalled()
        {
            return await Task.Run(() =>
            {
                if (_cachedIsZwiftInstalled.Item1)
                {
                    return _cachedIsZwiftInstalled.Item2;
                }

                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c reg query HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall /s";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                StringBuilder output = new StringBuilder();
                while (!process.StandardOutput.EndOfStream)
                {
                    output.AppendLine(process.StandardOutput.ReadLine());
                }
                process.WaitForExit();

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

                _cachedIsZwiftInstalled = new Tuple<bool, bool>(true, !string.IsNullOrEmpty(zwiftKey));
                _cachedZwiftKey = zwiftKey;

                return _cachedIsZwiftInstalled.Item2;
            });
        }


        /// <summary>
        /// 获取系统中Zwift安装位置
        /// </summary>
        /// <returns>Zwift安装位置</returns>
        public static async Task<string> GetInstallLocation()
        {
            return await Task.Run(async () =>
            {
                if (_cachedInstallLocation.Item1)
                {
                    return _cachedInstallLocation.Item2;
                }
                if (!await IsZwiftInstalled())
                {
                    _cachedInstallLocation = new Tuple<bool, string>(true, string.Empty);
                    return _cachedInstallLocation.Item2;
                }

                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c reg query \"{_cachedZwiftKey}\" /v InstallLocation";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                StringBuilder output = new StringBuilder();
                while (!process.StandardOutput.EndOfStream)
                {
                    output.AppendLine(process.StandardOutput.ReadLine());
                }
                process.WaitForExit();

                string result = output.ToString();
                int index = result.IndexOf("REG_SZ", StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                {
                    _cachedInstallLocation = new Tuple<bool, string>(true, result.Substring(index + 6).Trim());
                    return _cachedInstallLocation.Item2;
                }

                _cachedInstallLocation = new Tuple<bool, string>(true, string.Empty);
                _cachedIsZwiftInstalled = new Tuple<bool, bool>(true, false);
                return _cachedInstallLocation.Item2;
            });
        }

        /// <summary>
        /// 获取系统中安装的Zwift版本
        /// </summary>
        /// <returns>Zwift版本</returns>
        public static async Task<string> GetVersion()
        {
            if (_cachedVersion.Item1)
            {
                return _cachedVersion.Item2;
            }
            if (!await IsZwiftInstalled())
            {
                _cachedVersion = new Tuple<bool, string>(true, string.Empty);
                return _cachedVersion.Item2;
            }

            string filePath = Path.Combine(await GetInstallLocation(), "Zwift_ver_cur.xml");

            if (!File.Exists(filePath))
            {
                return "File does not exist.";
            }

            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(filePath);

                XmlElement root = document.DocumentElement;
                if (root.Name != "Zwift")
                {
                    return "Root element name error.";
                }

                return root.GetAttribute("version");
            }
            catch (Exception ex)
            {
                return $"Unknown error: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取系统中安装的Zwift版本 XML格式字符串
        /// </summary>
        /// <returns>Zwift版本 XML格式字符串</returns>
        public static async Task<string> GetXmlVersion()
        {
            if (_cachedVersion.Item1)
            {
                return _cachedVersion.Item2;
            }
            if (!await IsZwiftInstalled())
            {
                _cachedXmlVersion = new Tuple<bool, string>(true, string.Empty);
                return _cachedXmlVersion.Item2;
            }

            string filePath = Path.Combine(await GetInstallLocation(), "Zwift_ver_cur.xml");

            if (!File.Exists(filePath))
            {
                return "File does not exist.";
            }

            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(filePath);

                return document.OuterXml;
            }
            catch (Exception ex)
            {
                return $"Unknown error: {ex.Message}";
            }
        }
    }
}
