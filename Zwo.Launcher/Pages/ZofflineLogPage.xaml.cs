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
        public ZofflineLogPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ValueTuple<string, string> message && message.Item1 == "start")
            {
                await StartZofflineAsync(message.Item2);
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            await StartZofflineAsync("latest");
        }

        private async Task StartZofflineAsync(string version)
        {
            if (!ZofflineManager.IsStarted)
            {
                string latestLocalVersion = version;

                if (version == "latest")
                {
                    if (ZofflineManager.ShouldDownloadLatestVersion(out latestLocalVersion))
                    {
                        UpdateUI("正在下载最新版本 zoffline", true);
                        await ZofflineManager.DownloadZofflineAsync(ZofflineManager.GetLatestReleaseInfo(), LoadingProgressBar);
                    }
                }
                await ConfigureClient.RunConfigureClientAsync(await ZwiftManager.GetInstallLocationAsync(await ZwiftManager.GetZwiftKeyAsync()));
                ZofflineManager.RunZoffline(latestLocalVersion, LogRichEditBox);
                UpdateUI("运行中", false, true);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopZoffline();
        }

        private void StopZoffline()
        {
            ZofflineManager.StopZoffline();
            UpdateUI("未运行", false, false);
        }

        private void UpdateUI(string status, bool isProgressBarIndeterminate, bool enableStopButton = false)
        {
            ProcessStatusText.Text = status;
            LoadingProgressBar.IsIndeterminate = isProgressBarIndeterminate;
            StartButton.IsEnabled = !enableStopButton;
            StopButton.IsEnabled = enableStopButton;
        }
    }
}
