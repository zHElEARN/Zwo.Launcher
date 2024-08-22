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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Zwo.Launcher.Utils;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Zwo.Launcher.Pages.EnvInformationPage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ZofflineInformationFrame : Page
    {
        private List<ZofflineManager.ReleaseInfo> releaseInfoList;

        public ZofflineInformationFrame()
        {
            this.InitializeComponent();

            Task.Run(async () =>
            {
                try
                {
                    releaseInfoList = await ZofflineManager.GetReleaseInfosAsync();
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                   {
                         LoadingProgressBar.ShowError = true;
                         ContentDialog dialog = new ContentDialog();
                         dialog.XamlRoot = this.XamlRoot;
                         dialog.Title = "出现错误";
                         dialog.PrimaryButtonText = "复制信息并关闭";
                         dialog.CloseButtonText = "关闭";
                         dialog.Content = ex.Message;

                         var result = await dialog.ShowAsync();
                         if (result == ContentDialogResult.Primary)
                         {
                             var package = new DataPackage();
                             package.SetText(ex.Message);
                             Clipboard.SetContent(package);
                         }
                     });
                }

                ZofflineManager.MarkExistingZofflineFiles(releaseInfoList);

                DispatcherQueue.TryEnqueue(() =>
               {
                     ZofflineRemoteLatestText.Text = ZofflineManager.ParseZofflineVersion(releaseInfoList[0].TagName);
                     ZofflineVersionsDataGrid.ItemsSource = releaseInfoList;
                     LoadingProgressBar.IsIndeterminate = false;
                 });
            });
        }

        private async void DownloadSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            int index = ZofflineVersionsDataGrid.SelectedIndex;
            if (index != -1)
            {
                DownloadSelectedButton.IsEnabled = false;
                DownloadStatusText.Text = "下载中";
                LoadingProgressBar.IsIndeterminate = true;
                await ZofflineManager.DownloadZofflineAsync(releaseInfoList[index], LoadingProgressBar);
                ZofflineManager.MarkExistingZofflineFiles(releaseInfoList);

                DispatcherQueue.TryEnqueue(() =>
                {
                    DownloadSelectedButton.IsEnabled = true;
                    DownloadStatusText.Text = "";
                    ZofflineVersionsDataGrid.ItemsSource = null;
                    ZofflineVersionsDataGrid.ItemsSource = releaseInfoList;
                });
            }
        }
    }

    public class BooleanToStringConverter : IValueConverter
    {
        public string TrueContent { get; set; }
        public string FalseContent { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? TrueContent : FalseContent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
