using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

            ZwiftManager.GetInstallLocation().ContinueWith(task =>
            {
                string zwiftInstallLocation = task.Result;

                DispatcherQueue.TryEnqueue(async () => {
                    bool isZwiftInstalled = await ZwiftManager.IsZwiftInstalled();

                    ZwiftProgressBar.IsIndeterminate = false;
                    ZwiftProgressBar.ShowError = !isZwiftInstalled;

                    ZwiftStatusText.Text = isZwiftInstalled ? "已安装" : "未安装";
                    ZwiftLocationText.Text = isZwiftInstalled ? zwiftInstallLocation : "未安装";
                    ZwiftVersionText.Text = isZwiftInstalled ? await ZwiftManager.GetVersion() : "未安装";
                    DetailedVersionText.Text = isZwiftInstalled ? await ZwiftManager.GetXmlVersion() : "未安装";
                    DetailedVersionExpander.IsEnabled = isZwiftInstalled;
                });
            });
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
