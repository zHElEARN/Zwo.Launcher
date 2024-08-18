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
    public sealed partial class StartPage : Page
    {
        public StartPage()
        {
            this.InitializeComponent();

            Task.Run(async () =>
            {
                var latestReleaseInfo = ZofflineManager.GetLatestReleaseInfo();
                var zofflineVersion = ZofflineManager.ParseZofflineVersion(latestReleaseInfo.TagName);

                var zwiftKey = await ZwiftManager.GetZwiftKeyAsync();
                var isZwiftInstalled = !string.IsNullOrEmpty(zwiftKey);

                DispatcherQueue.TryEnqueue(async () =>
                {
                    if (isZwiftInstalled)
                    {
                        var zwiftInstallLocation = await ZwiftManager.GetInstallLocationAsync(zwiftKey);
                        var zwiftVersion = ZwiftManager.GetVersion(zwiftInstallLocation);

                        ZwiftVersion.Text = zwiftVersion;
                    }
                    else
                    {
                        ZwiftVersion.Text = "未安装";
                    }
                    ZofflineVersion.Text = zofflineVersion;
                    LoadingProgressBar.IsIndeterminate = false;
                });
            });

            if (ZofflineManager.IsStarted)
            {
                StartButtonText.Text = "正在运行";
                StartButtonFontIcon.Glyph = "\uF0AF";
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (Application.Current as App)?.m_window as MainWindow;
            mainWindow.Navigate(typeof(ZofflineLogPage), 3, ZofflineManager.IsStarted ? null : ("start", "latest"));
        }
    }
}
