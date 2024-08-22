using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Zwo.Launcher.Utils
{
    public static class ConfigurationManager
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".zlauncher\config.json");

        public static Configuration Config { get; private set; }

        public static void LoadConfiguration()
        {
            try
            {
                string configJson = File.ReadAllText(ConfigFilePath);
                Config = JsonSerializer.Deserialize<Configuration>(configJson);
            }
            catch (Exception)
            {
                Config = new Configuration();
            }
        }

        public static void SaveConfiguration()
        {
            string configJson = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, configJson);
        }
    }

    public class Configuration
    {
        [JsonPropertyName("configurationMode")]
        public string ConfigurationMode { get; set; } = "auto";

        [JsonPropertyName("proxySettings")]
        public ProxySettings ProxySettings { get; set; } = new ProxySettings();

        [JsonPropertyName("downloadAcceleration")]
        public DownloadAcceleration DownloadAcceleration { get; set; } = new DownloadAcceleration();
    }

    public class ProxySettings
    {
        [JsonPropertyName("enabled")]
        public bool IsEnabled { get; set; } = false;

        [JsonPropertyName("useSystemSettings")]
        public bool IsUseSystemSettings { get; set; } = true;

        [JsonPropertyName("proxyServerAddress")]
        public string ProxyServerAddress { get; set; } = "http://127.0.0.1:7890";
    }

    public class DownloadAcceleration
    {
        [JsonPropertyName("enabled")]
        public bool IsEnabled { get; set; } = true;

        [JsonPropertyName("mirror")]
        public string Mirror { get; set; } = "https://mirror.ghproxy.com/";
    }
}
