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
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using zlauncher.Zwift;
using Zwo.Launcher.Utils;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Zwo.Launcher.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ZofflineLogPage : Page
    {
        private readonly HostsManager.HostsEntry zwiftHostsEntry = new("127.0.0.1", ["us-or-rly101.zwift.com", "secure.zwift.com", "cdn.zwift.com", "launcher.zwift.com"]);

        public ZofflineLogPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ValueTuple<string, string> message && message.Item1 == "start")
            {
                await StartAsync(message.Item2);
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            await StartAsync("latest");
        }

        private async Task StartAsync(string version)
        {
            if (!ZofflineManager.IsStarted)
            {
                string latestLocalVersion = version;

                if (version == "latest")
                {
                    if (ZofflineManager.ShouldDownloadLatestVersion(out latestLocalVersion))
                    {
                        var latestReleaseInfo = ZofflineManager.GetLatestReleaseInfo();
                        latestLocalVersion = ZofflineManager.ParseZofflineVersion(latestReleaseInfo.TagName);

                        UpdateUI("正在下载最新版本 zoffline", true, false, false);
                        await ZofflineManager.DownloadZofflineAsync(latestReleaseInfo, LoadingProgressBar);
                    }
                }
                HostsManager.AddEntry(zwiftHostsEntry);
                await ConfigureClient.RunConfigureClientAsync(await ZwiftManager.GetInstallLocationAsync(await ZwiftManager.GetZwiftKeyAsync()));
                ZofflineManager.RunZoffline(latestLocalVersion, LogRichEditBox, async () =>
                {
                    ZwiftManager.RunZwift(await ZwiftManager.GetInstallLocationAsync(await ZwiftManager.GetZwiftKeyAsync()));
                });
                UpdateUI("运行中", false, false, true);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopZoffline();
        }

        private void StopZoffline()
        {
            ZofflineManager.StopZoffline();
            HostsManager.RemoveAllEntries();
            ZwiftManager.StopZwift();
            UpdateUI("未运行", false, true, false);
        }

        private void UpdateUI(string status, bool isProgressBarIndeterminate, bool enableStartButton, bool enableStopButton)
        {
            ProcessStatusText.Text = status;
            LoadingProgressBar.IsIndeterminate = isProgressBarIndeterminate;
            StartButton.IsEnabled = enableStartButton;
            StopButton.IsEnabled = enableStopButton;
        }
    }
}
