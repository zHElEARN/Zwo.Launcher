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
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            ConfigurationManager.LoadConfiguration();

            ConfigurationModeComboBox.SelectedIndex = ConfigurationManager.Config.ConfigurationMode == "auto" ? 0 : 1;

            ProxySettingsToggleSwitch.IsOn = ConfigurationManager.Config.ProxySettings.IsEnabled;
            UseSystemSettingsToggleSwitch.IsOn = ConfigurationManager.Config.ProxySettings.IsUseSystemSettings;
            ProxyServerAddressTextBox.Text = ConfigurationManager.Config.ProxySettings.ProxyServerAddress;

            DownloadAccelerationToggleSwitch.IsOn = ConfigurationManager.Config.DownloadAcceleration.IsEnabled;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ConfigurationManager.Config.ConfigurationMode = ConfigurationModeComboBox.SelectedIndex == 0 ? "auto" : "manual";
            
            ConfigurationManager.Config.ProxySettings.IsEnabled = ProxySettingsToggleSwitch.IsOn;
            ConfigurationManager.Config.ProxySettings.IsUseSystemSettings = UseSystemSettingsToggleSwitch.IsOn;
            ConfigurationManager.Config.ProxySettings.ProxyServerAddress = ProxyServerAddressTextBox.Text;

            ConfigurationManager.Config.DownloadAcceleration.IsEnabled = DownloadAccelerationToggleSwitch.IsOn;

            ConfigurationManager.SaveConfiguration();
        }
    }
}
