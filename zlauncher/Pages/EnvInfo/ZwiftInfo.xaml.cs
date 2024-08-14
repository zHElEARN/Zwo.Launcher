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
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using zlauncher.Zwift;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace zlauncher.Pages.EnvInfo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ZwiftInfo : Page
    {
        public ZwiftInfo()
        {
            this.InitializeComponent();

            ZwiftManager.GetInstallLocation().ContinueWith(task =>
            {
                string ZwiftInstallLocation = task.Result;

                DispatcherQueue.TryEnqueue(() => {
                    bool IsZwiftInstalled = !string.IsNullOrEmpty(ZwiftInstallLocation);

                    progressBar.IsIndeterminate = false;
                    progressBar.ShowError = !IsZwiftInstalled;

                    Status.Text = IsZwiftInstalled ? "已安装" : "未安装";
                    Location.Text = IsZwiftInstalled ? ZwiftInstallLocation : "未安装";
                    Version.Text = IsZwiftInstalled ? ZwiftManager.GetVersion(ZwiftInstallLocation) : "未安装";
                    DetailedVersion.Text = IsZwiftInstalled ? ZwiftManager.GetXmlVersion(ZwiftInstallLocation) : "未安装";
                });
            });
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(DetailedVersion.Text);
            Clipboard.SetContent(package);
            ToggleCopyTeachingTip.IsOpen = true;
        }
    }
}
