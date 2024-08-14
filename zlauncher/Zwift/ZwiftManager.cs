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
    class ZwiftManager
    {
        /// <summary>
        /// 获取Zwift安装目录
        /// </summary>
        /// <param name="installLocation">输出 安装目录</param>
        /// <returns>是否安装Zwift</returns>
        public static async Task<string> GetInstallLocation()
        {
            return await Task.Run(() =>
            {
                string installLocation = null;

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

                if (!string.IsNullOrEmpty(zwiftKey))
                {
                    process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/c reg query \"{zwiftKey}\" /v InstallLocation";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    output.Clear();
                    while (!process.StandardOutput.EndOfStream)
                    {
                        output.AppendLine(process.StandardOutput.ReadLine());
                    }
                    process.WaitForExit();

                    string result = output.ToString();
                    int index = result.IndexOf("REG_SZ", StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                    {
                        installLocation = result.Substring(index + 6).Trim();
                        return installLocation;
                    }
                }

                return string.Empty;
            });
        }

        /// <summary>
        /// 获取安装的Zwift版本
        /// </summary>
        /// <param name="zwiftLocation">Zwift安装目录</param>
        /// <returns>Zwift版本</returns>
        public static string GetVersion(string zwiftLocation)
        {
            string version = string.Empty;
            string filePath = Path.Combine(zwiftLocation, "Zwift_ver_cur.xml");

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

                version = root.GetAttribute("version");
            }
            catch (Exception ex)
            {
                return $"Unknown error: {ex.Message}";
            }

            return version;
        }

        /// <summary>
        /// 获取安装的Zwift版本 XML格式字符串
        /// </summary>
        /// <param name="zwiftLocation">Zwift安装目录</param>
        /// <returns>Zwift版本 XML格式字符串</returns>
        public static string GetXmlVersion(string zwiftLocation)
        {
            string filePath = Path.Combine(zwiftLocation, "Zwift_ver_cur.xml");

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
