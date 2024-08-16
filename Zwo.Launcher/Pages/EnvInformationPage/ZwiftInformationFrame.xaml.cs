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
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using zlauncher.Zwift;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Zwo.Launcher.Pages.EnvInformationPage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ZwiftInformationFrame : Page
    {
        public ZwiftInformationFrame()
        {
            this.InitializeComponent();

            new Thread(() =>
            {
                string zwiftKey = ZwiftManager.GetZwiftKey();
                bool isZwiftInstalled = !string.IsNullOrEmpty(zwiftKey);
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (isZwiftInstalled)
                    {
                        string zwiftInstallLocation = ZwiftManager.GetInstallLocation(zwiftKey);

                        ZwiftStatusText.Text = "已安装";
                        ZwiftLocationText.Text = zwiftInstallLocation;
                        ZwiftVersionText.Text = ZwiftManager.GetVersion(zwiftInstallLocation);
                        DetailedVersionText.Text = ZwiftManager.GetXmlVersion(zwiftInstallLocation);
                        DetailedVersionExpander.IsEnabled = true;
                    }
                    else
                    {
                        ZwiftProgressBar.ShowError = true;
                        ZwiftStatusText.Text = "未安装";
                        ZwiftLocationText.Text = "未安装";
                        ZwiftVersionText.Text = "未安装";
                        DetailedVersionText.Text = "未安装";
                    }
                    ZwiftProgressBar.IsIndeterminate = false;
                });
            }).Start();
        }

        private void CopyDetailedButton_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(DetailedVersionText.Text);
            Clipboard.SetContent(package);
            CopyDetailedVersionTeachingTip.IsOpen = true;
        }
    }
}
