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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Zwo.Launcher.Pages;
using Zwo.Launcher.Pages.EnvInformationPage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Zwo.Launcher
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
        }

        public void Navigate(Type pageType, int index = -1, object parameter = null)
        {
            ContentFrame.Navigate(pageType, parameter);
            if (index != -1)
            {
                NavView.SelectedItem = NavView.MenuItems[index];
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                Navigate(typeof(SettingsPage));
            }
            else
            {
                NavigationViewItem selectedItem = args.SelectedItem as NavigationViewItem;
                string selectedTag = selectedItem.Tag as string;
                switch (selectedTag)
                {
                    case "Start":
                        Navigate(typeof(StartPage));
                        break;
                    case "EnvInformation":
                        Navigate(typeof(EnvInformationPage));
                        break;
                    case "ZofflineLog":
                        Navigate(typeof(ZofflineLogPage));
                        break;
                }
            }
        }
    }
}
