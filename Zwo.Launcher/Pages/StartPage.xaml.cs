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
using zlauncher.Zwift;
using Zwo.Launcher.Utils;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Zwo.Launcher.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        public StartPage()
        {
            this.InitializeComponent();

            new Thread(() =>
            {
                var latestReleaseInfo = ZofflineManager.GetLatestReleaseInfo();
                var zofflineVersion = ZofflineManager.ParseZofflineVersion(latestReleaseInfo.TagName);

                var zwiftKey = ZwiftManager.GetZwiftKey();
                var isZwiftInstalled = !string.IsNullOrEmpty(zwiftKey);

                DispatcherQueue.TryEnqueue(() =>
                {
                    if (isZwiftInstalled)
                    {
                        var zwiftInstallLocation = ZwiftManager.GetInstallLocation(zwiftKey);
                        var zwiftVersion = ZwiftManager.GetVersion(zwiftInstallLocation);

                        ZwiftVersion.Text = zwiftVersion;
                    }
                    else
                    {
                        ZwiftVersion.Text = "Î´°²×°";
                    }
                    ZofflineVersion.Text = zofflineVersion;
                    LoadingProgressBar.IsIndeterminate = false;
                });
            }).Start();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var latestReleaseInfo = ZofflineManager.GetLatestReleaseInfo();
            var latestLocalVersion = ZofflineManager.GetLocalLatestVersion();

            if (string.IsNullOrEmpty(latestLocalVersion) || ZofflineManager.CompareVersions(ZofflineManager.ParseZofflineVersion(latestReleaseInfo.TagName), latestLocalVersion) > 0)
            {
                VersionTeachingTip.IsOpen = true;
                new Thread(() =>
                {
                    Thread.Sleep(2000);
                    VersionTeachingTip.DispatcherQueue.TryEnqueue(() => VersionTeachingTip.IsOpen = false);
                }).Start();

                LoadingProgressBar.IsIndeterminate = true;
                new Thread(async () =>
                {
                    await ZofflineManager.DownloadZofflineAsync(latestReleaseInfo, LoadingProgressBar);
                    var mainWindow = (Application.Current as App)?.m_window as MainWindow;
                    mainWindow.DispatcherQueue.TryEnqueue(() => mainWindow.Navigate(typeof(ZofflineLogPage), 3, ("start", latestLocalVersion)));
                }).Start();
            }
            else
            {
                var mainWindow = (Application.Current as App)?.m_window as MainWindow;
                mainWindow.Navigate(typeof(ZofflineLogPage), 3, ("start", latestLocalVersion));
            }
        }
    }
}
