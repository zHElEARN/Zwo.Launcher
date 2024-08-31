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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Isolation;
using Zwo.Launcher.Pages;
using Zwo.Launcher.Pages.EnvInformationPage;
using Zwo.Launcher.Utils;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Zwo.Launcher
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static WinProc newWndProc = null;
        private static IntPtr oldWndProc = IntPtr.Zero;
        private delegate IntPtr WinProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        internal static extern int GetDpiForWindow(IntPtr hWnd);

        [DllImport("User32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("User32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("User32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        public static int MinWindowWidth { get; set; } = 1100;
        public static int MaxWindowWidth { get; set; } = 0;
        public static int MinWindowHeight { get; set; } = 600;
        public static int MaxWindowHeight { get; set; } = 0;

        private static void RegisterWindowMinMax(Window window)
        {
            var hwnd = GetWindowHandleForCurrentWindow(window);

            newWndProc = new WinProc(WndProc);
            oldWndProc = SetWindowLongPtr(hwnd, WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
        }

        private static IntPtr GetWindowHandleForCurrentWindow(object target) => WinRT.Interop.WindowNative.GetWindowHandle(target);

        private static IntPtr WndProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case WindowMessage.WM_GETMINMAXINFO:
                    var dpi = GetDpiForWindow(hWnd);
                    var scalingFactor = (float)dpi / 96;

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.x = (int)(MinWindowWidth * scalingFactor);
                    // minMaxInfo.ptMaxTrackSize.x = (int)(MaxWindowWidth * scalingFactor);
                    minMaxInfo.ptMinTrackSize.y = (int)(MinWindowHeight * scalingFactor);
                    // minMaxInfo.ptMaxTrackSize.y = (int)(MaxWindowHeight * scalingFactor);

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;
            }

            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            }
            else
            {
                return new IntPtr(SetWindowLong32(hWnd, nIndex, newProc));
            }
        }

        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [Flags]
        private enum WindowLongIndexFlags : int
        {
            GWL_WNDPROC = -4
        }

        private enum WindowMessage : int
        {
            WM_GETMINMAXINFO = 0x0024
        }

        private bool _isExited = false;

        public MainWindow()
        {
            this.InitializeComponent();
            RegisterWindowMinMax(this);

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            this.Closed += MainWindow_Closed;
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (!ZofflineManager.IsStarted || _isExited)
            {
                base.Close();
                return;
            }
            else
            {
                args.Handled = true;

                var dialog = new ContentDialog
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = "确认关闭",
                    Content = "zoffline 未退出，你确定要退出启动器吗?",
                    PrimaryButtonText = "是",
                    SecondaryButtonText = "否"
                };

                dialog.PrimaryButtonClick += (s, _) =>
                {
                    ZofflineManager.StopZoffline();
                    HostsManager.RemoveAllEntries();

                    _isExited = true;
                    base.Close();
                };

                await dialog.ShowAsync();
            }
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
                    case "Tools":
                        Navigate(typeof(ToolsPage));
                        break;
                }
            }
        }
    }
}
