using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zwo.Launcher.Utils
{
    static class ConfigureClient
    {
        public static async Task RunConfigureClientAsync(string zwiftPath)
        {
            Debug.WriteLine(zwiftPath);

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".zlauncher");
            string fileName = "configure_client.bat";
            string filePath = Path.Combine(folderPath, fileName);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"\"{zwiftPath}\"",
            };

            using var process = Process.Start(processStartInfo);
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            Debug.WriteLine(output);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.WriteLine($"Error: {error}");
            }
        }
    }
}
