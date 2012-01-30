using System.Windows;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace SBW2
{
    /// <summary>
    /// Interaction logic for NewRemoteUser.xaml
    /// </summary>
    public partial class NewRemoteUser : Window
    {
        public NewRemoteUser()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("Please enter text in the textbox");
                textBox1.Focus();
                return;
            }
            Config.UserCache[textBox1.Text] = "0";
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableGlass();
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
    }
}
