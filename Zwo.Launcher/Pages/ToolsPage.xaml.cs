using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Zwo.Launcher.Utils;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Zwo.Launcher.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ToolsPage : Page
    {
        public ToolsPage()
        {
            this.InitializeComponent();
        }

        private void EditHostsSettingsCard_Click(object sender, RoutedEventArgs e)
        {
            string hostsFilePath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\drivers\etc\hosts");

            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = hostsFilePath,
                UseShellExecute = true,
                Verb = "runas"
            });
        }

        private async void FixHostsSettingsCard_Click(object sender, RoutedEventArgs e)
        {
            HostsManager.RemoveAllEntries();

            var externalEntries = HostsManager.GetEntriesOutsideBlock();
            foreach (var entry in externalEntries)
            {
                if (entry.Domains.Exists(domain => domain.Contains("zwift", StringComparison.OrdinalIgnoreCase)))
                {
                    HostsManager.RemoveEntryOutsideBlock(entry);
                }
            }

            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = "已修复",
                Content = "已删除 Hosts 文件中已存在的 Zoffline 配置",
                PrimaryButtonText = "OK",
            };

            await dialog.ShowAsync();
        }
    }
}
