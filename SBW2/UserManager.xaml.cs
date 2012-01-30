using System.Collections.Generic;
using System.Windows;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace SBW2
{
    public partial class UserManager : Window
    {
        public UserManager()
        {
            InitializeComponent();

            dataGrid1.ItemsSource = new List<UserItem>(UserCollection.Get);
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.UserCache.Save();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            new NewRemoteUser().ShowDialog();

            dataGrid1.ItemsSource = new List<UserItem>(UserCollection.Get);
        }

        private void EnableGlass()
        {
            if (Environment.OSVersion.Version.Major < 6 || !DwmIsCompositionEnabled()) return;
            // Get the current window handle
            var mainWindowPtr = new WindowInteropHelper(this).Handle;
            var mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
            if (mainWindowSrc != null)
                if (mainWindowSrc.CompositionTarget != null)
                    mainWindowSrc.CompositionTarget.BackgroundColor = Colors.Transparent;
                else return;
            else return;

            Background = Brushes.Transparent;

            // Set the proper margins for the extended glass part
            var margins = new Margins { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };

            DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMargins);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern bool DwmIsCompositionEnabled();

        [StructLayout(LayoutKind.Sequential)]
        private class Margins
        {
            public int cxLeftWidth, cxRightWidth,
                cyTopHeight, cyBottomHeight;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableGlass();
        }

        private void dataGrid1_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            e.Row.Background = new SolidColorBrush(Color.FromArgb(127, 255, 255, 255));
        }
    }
}
