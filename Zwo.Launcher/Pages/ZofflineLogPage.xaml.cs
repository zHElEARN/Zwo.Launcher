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
    public sealed partial class ZofflineLogPage : Page
    {
        public ZofflineLogPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ValueTuple<string, string> message)
            {
                if (message.Item1 == "start")
                {
                    if (!ZofflineManager.IsStarted)
                    {
                        ZofflineManager.RunZoffline(message.Item2, LogRichEditBox);

                        ProcessStatusText.Text = "运行中";
                        StartButton.IsEnabled = false;
                        StopButton.IsEnabled = true;
                    }
                }
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ZofflineManager.IsStarted)
            {
                var latestReleaseInfo = ZofflineManager.GetLatestReleaseInfo();
                var latestLocalVersion = ZofflineManager.GetLocalLatestVersion();

                if (string.IsNullOrEmpty(latestLocalVersion) || ZofflineManager.CompareVersions(ZofflineManager.ParseZofflineVersion(latestReleaseInfo.TagName), latestLocalVersion) > 0)
                {
                    LoadingProgressBar.IsIndeterminate = true;
                    ProcessStatusText.Text = "正在下载最新版本 zoffline";
                    await ZofflineManager.DownloadZofflineAsync(latestReleaseInfo, LoadingProgressBar);

                    ZofflineManager.RunZoffline(latestLocalVersion, LogRichEditBox);

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ProcessStatusText.Text = "运行中";
                        StartButton.IsEnabled = false;
                        StopButton.IsEnabled = true;
                    });
                }
                else
                {
                    ZofflineManager.RunZoffline(latestLocalVersion, LogRichEditBox);

                    ProcessStatusText.Text = "运行中";
                    StartButton.IsEnabled = false;
                    StopButton.IsEnabled = true;
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (ZofflineManager.IsStarted)
            {
                ZofflineManager.StopZoffline();

                ProcessStatusText.Text = "未运行";
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            }
        }
    }
}
