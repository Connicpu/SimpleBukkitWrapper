/* Microsoft Public License (Ms-PL)
 * 
 *  1.Definitions
 *      The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same
 *      meaning here as under U.S. copyright law.
 *      A "contribution" is the original software, or any additions or changes to the software.
 *      A "contributor" is any person that distributes its contribution under this license.
 *      "Licensed patents" are a contributor's patent claims that read directly on its contribution.

 * 2.Grant of Rights
 *  (A) Copyright Grant- Subject to the terms of this license, including the license conditions
 *      and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free
 *      copyright license to reproduce its contribution, prepare derivative works of its contribution,
 *      and distribute its contribution or any derivative works that you create.
 *  (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations
 *      in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its
 *      licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its
 *      contribution in the software or derivative works of the contribution in the software.

 * 3.Conditions and Limitations
 *  (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
 *  (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software,
 *      your patent license from such contributor to the software ends automatically.
 *  (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution
 *      notices that are present in the software.
 *  (D) If you distribute any portion of the software in source code form, you may do so only under this license by
 *      including a complete copy of this license with your distribution. If you distribute any portion of the software
 *      in compiled or object code form, you may only do so under a license that complies with this license.
 *  (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties,
 *      guarantees, or conditions. You may have additional consumer rights under your local laws which this license
 *      cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties
 *      of merchantability, fitness for a particular purpose and non-infringement.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SBW2.ServerHandler;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Threading.Timer;

namespace SBW2
{
    public partial class MainWindow
    {
        internal static MainWindow Instance;
        internal readonly NotifyIcon Noticon;
        private readonly ConsoleStream _consoleStream;
        private readonly bool non_startup = false;
        private Timer s_stats_update;

        public MainWindow()
        {
            InitializeComponent();

            for (var i = 512; i <= 4096; i += 512)
            {
                MemoryComboBox.Items.Add(i + "M");
            }

            memboxGal.SelectedItem = Config.Default["MaxHeap"];

            try
            {
                Noticon = new NotifyIcon {Icon = Properties.Resources.bukkit, Visible = true};
                Noticon.DoubleClick += delegate
                                           {
                                               if (Visibility != Visibility.Visible)
                                                   Show();
                                           };

                var notimenu = new ContextMenuStrip();

                var startMi = new ToolStripMenuItem("Start") {Image = Properties.Resources.start};
                startMi.Click += Commands.StartServer.Execute;

                var stopMi = new ToolStripMenuItem("Stop") {Image = Properties.Resources.stop};
                stopMi.Click += Commands.StopServer.Execute;

                var restartMi = new ToolStripMenuItem("Restart") {Image = Properties.Resources.restart};
                restartMi.Click += Commands.RestartServer.Execute;

                notimenu.Items.Add(startMi);

                Noticon.ContextMenuStrip = notimenu;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            s_stats_update = new Timer(delegate
                          {
                              Dispatcher.BeginInvoke(
                                  new Action(delegate
                                                 {
                                                     MemStats.Text = "Memory: " +
                                                                     ProcessHandler
                                                                         .Instance.
                                                                         WorkingSet/
                                                                     1048576 + " MB";
                                                     Uptime.Text = "Uptime: " +
                                                                   UptimeString();
                                                 }));
                          }, null, 100, 1000);

            Ribbon_Status.Text = "Server is\nStopped";
            ProcessHandler.Output +=
                (sender, data) =>
                    {
                        if (String.IsNullOrEmpty(data)) return;

                        if (ReferenceEquals(sender, ProcessHandler.ConsoleError))
                            Dispatcher.BeginInvoke(new Action(
                                                       () =>
                                                           {
                                                               var para = new Paragraph();

                                                               switch (data[10])
                                                               {
                                                                   case 'W':
                                                                       para.Foreground =
                                                                           new SolidColorBrush(Colors.DarkOrange);
                                                                       para.Inlines.Add(data);
                                                                       break;
                                                                   case 'S':
                                                                       para.Foreground =
                                                                           new SolidColorBrush(Colors.Red);
                                                                       para.Inlines.Add(data);
                                                                       break;
                                                                   case 'I':
                                                                       para = (Paragraph) ParseAnsiColor(data);
                                                                       break;
                                                                   default:
                                                                       para.Foreground =
                                                                           new SolidColorBrush(Colors.Black);
                                                                       para.Inlines.Add(data);
                                                                       break;
                                                               }

                                                               para.LineHeight = 0.1;

                                                               outputBox.Document.Blocks.Add(para);
                                                           }));
                        else if (ReferenceEquals(sender, ProcessHandler.ConsoleOutput))
                            Dispatcher.BeginInvoke(new Action(
                                                       () =>
                                                       {
                                                           var para = new Paragraph
                                                                          {
                                                                              Foreground =
                                                                                  new SolidColorBrush(
                                                                                  Colors.Black),
                                                                              LineHeight = 0.1
                                                                          };
                                                           para.Inlines.Add(data);
                                                           outputBox.Document.Blocks.Add(para);
                                                       }));
                        else
                            Dispatcher.BeginInvoke(new Action(
                                                       () =>
                                                       {
                                                           var para = new Paragraph
                                                                          {
                                                                              Foreground =
                                                                                  new SolidColorBrush(
                                                                                  Color.FromRgb(0, 127, 0)),
                                                                              LineHeight = 0.1
                                                                          };
                                                           para.Inlines.Add(data);
                                                           outputBox.Document.Blocks.Add(para);
                                                       }));

                        Dispatcher.BeginInvoke(new Action(() => outputBox.ScrollToEnd()));
                    };
            ProcessHandler.Instance.StateChanged +=
                delegate(bool newstate)
                    {
                        if (newstate)
                        {
                            Dispatcher.BeginInvoke(
                                new Action(
                                    delegate
                                        {
                                            StartRB.IsEnabled = false;
                                            StopRB.IsEnabled = true;
                                            RestartRB.IsEnabled = true;

                                            StartStopQAB.SmallImageSource = new BitmapImage
                                                (new Uri
                                                     ("/Images/stop.png",
                                                      UriKind.Relative));
                                            StartStopQAB.Command = Commands.StopServer;

                                            Ribbon_Status.Text = "Server is\nRunning";

                                            StartThumb.IsEnabled = false;
                                            StopThumb.IsEnabled = true;
                                            RestartThumb.IsEnabled = true;
                                        }));
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(
                                new Action(
                                    delegate
                                        {
                                            StartRB.IsEnabled = true;
                                            StopRB.IsEnabled = false;
                                            RestartRB.IsEnabled = false;

                                            StartStopQAB.SmallImageSource = new BitmapImage
                                                (new Uri("/Images/start.png",
                                                         UriKind.Relative));
                                            StartStopQAB.Command = Commands.StartServer;

                                            Ribbon_Status.Text = "Server is\nStopped";

                                            StartThumb.IsEnabled = true;
                                            StopThumb.IsEnabled = false;
                                            RestartThumb.IsEnabled = false;
                                        }));
                        }
                    };

            if (Config.NetConf["enabled"] == "true")
            {
                int port;
                if (!int.TryParse(Config.NetConf["port"], out port))
                    port = 3000;

                //Net.Listener.StartListening(port);
                NetworkToggle.IsChecked = true;
            }

            Instance = this;

            memcb.IsEditable = true;
            memcb.Text = Config.Default["maxheap"];

            _consoleStream = new ConsoleStream(this);

            non_startup = true;
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var cmd = inputBox.Text;

            Net.CommandHandlers.ProcessCommand(cmd, _consoleStream, int.MaxValue, "Console", Encoding.Unicode);

            inputBox.Text = "";
        }

        private static string UptimeString()
        {
            var uptime = ProcessHandler.Instance.Uptime;

            if (uptime < 3600)
            {
                string sc;
                if (uptime%60 < 10)
                {
                    sc = "0" + uptime%60;
                }
                else
                {
                    sc = "" + uptime%60;
                }
                return ((uptime - (uptime%60))/60) + ":" + sc;
            }
            string mc;
            if (uptime%3600 < 600)
            {
                mc = "0" + ((uptime - (uptime%60))/60)%60;
            }
            else
            {
                mc = "" + ((uptime - (uptime%60))/60)%60;
            }
            return ((uptime - (uptime%3600))/3600) + ":" + mc;
        }

        private void RibbonWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }

        private static Block ParseAnsiColor(string data)
        {
            var reader = new StringReader(data);
            var temp = "";
            var color = Colors.Black;

            var p = new Paragraph();

            while (true)
            {
                int character;
                if ((character = reader.Read()) == -1)
                {
                    var pElement = new Run(temp) {Foreground = new SolidColorBrush(color)};
                    p.Inlines.Add(pElement);
                    break;
                }

                if ((char) character == '\u001B')
                {
                    var pElement = new Run(temp) {Foreground = new SolidColorBrush(color)};
                    p.Inlines.Add(pElement);

                    temp = "";

                    reader.Read();

                    var colorstring = "";
                    char tempchar;

                    while ((tempchar = (char) reader.Read()) != 'm')
                    {
                        if (tempchar == 'm') break;
                        if (tempchar == '[') break;
                        colorstring += tempchar;
                    }
                    int colorcode;
                    var pr = int.TryParse(colorstring, out colorcode);
                    if (!pr) continue;

                    var nxtclr = ParseAnsiColorCode(colorcode);
                    if (nxtclr == null) continue;

                    color = nxtclr.Value;
                }
                else
                {
                    temp += (char) character;
                }
            }

            return p;
        }

        private static Color? ParseAnsiColorCode(int code)
        {
            switch (code)
            {
                case 30:
                    return Colors.Black;
                case 31:
                    return Colors.Red;
                case 32:
                    return Colors.Green;
                case 33:
                    return Colors.DarkOrange;
                case 34:
                    return Colors.Blue;
                case 35:
                    return Colors.DarkMagenta;
                case 36:
                    return Colors.DarkCyan;
                case 37:
                    return Colors.Black;
                default:
                    return null;
            }
        }

        private void MemoryComboBoxSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            memcb.Text = (string) memboxGal.SelectedValue;
        }

        private void NetToggleChecked(object sender, RoutedEventArgs e)
        {
            if (non_startup)
            {
                var result = MessageBox.Show("Are you sure you want to enable networking with the current settings?" +
                                             " If not properly configured, it may be possible for an attacker to enter your server.",
                                             "Simple Bukkit Wrapper", MessageBoxButton.YesNo, MessageBoxImage.Warning,
                                             MessageBoxResult.No);
                if (result == MessageBoxResult.No)
                {
                    NetworkToggle.IsChecked = false;
                    return;
                }
            }

            Config.NetConf["enabled"] = "true";

            int port;
            if (!int.TryParse(Config.NetConf["port"], out port))
                port = 3000;

            Net.Listener.StartListening(port);
        }

        private void NetworkToggleUnchecked(object sender, RoutedEventArgs e)
        {
            if (non_startup)
            {
                var result =
                    MessageBox.Show("Are you sure you wish to disable all networking to your server? It will " +
                                    "be impossible to connect to it remotely and any existing connections will be closed.",
                                    "", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (result == MessageBoxResult.No)
                {
                    NetworkToggle.IsChecked = true;
                    return;
                }
            }

            Config.NetConf["enabled"] = "false";
            Net.Listener.StopListening();
        }

        private class ConsoleStream : Stream
        {
            private readonly MainWindow _;

            internal ConsoleStream(MainWindow parent)
            {
                _ = parent;
            }

            public override void Flush()
            {
                
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return 0;
            }

            public override void SetLength(long value)
            {

            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return 0;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _.outputBox.Document.Blocks.Add(ParseAnsiColor(
                    new UnicodeEncoding().GetString(buffer, offset, count)));
            }

            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { return 1000000; }
            }

            public override long Position
            {
                get { return 0; }
                set {  }
            }
        }
    }
}