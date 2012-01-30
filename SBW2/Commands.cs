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
using System.Windows.Input;
using System.Threading;

namespace SBW2
{
    public static class Commands
    {
        private static PortForward _portForward;
        private static UserManager _userManager;

        public static Command StartServer
        {
            get
            {
                return new Command(ServerHandler.ProcessHandler.Instance.Start);
            }
        }

        public static Command StopServer
        {
            get
            {
                return new Command(
                    () =>
                        {
                            MainWindow.Instance.Dispatcher.Invoke(new Action(() =>
                                                                                 {
                                                                                     MainWindow.Instance.StopRB.IsEnabled = false;
                                                                                     MainWindow.Instance.RestartRB.IsEnabled = false;
                                                                                     MainWindow.Instance.Noticon.ContextMenuStrip.Enabled = false;
                                                                                     MainWindow.Instance.StartStopQAB.IsEnabled = false;
                                                                                     MainWindow.Instance.StopThumb.IsEnabled = false;
                                                                                     MainWindow.Instance.RestartThumb.IsEnabled = false;
                                                                                 }));
                            ServerHandler.ProcessHandler.Instance.Stop();
                            new Thread(() =>
                                           {
                                               ServerHandler.ProcessHandler.Instance.
                                                   WaitForExit();

                                               MainWindow.Instance.Noticon.
                                                   ContextMenuStrip.Enabled = true;
                                               MainWindow.Instance.Dispatcher.Invoke(
                                                   new Action(() =>
                                                                  {
                                                                      MainWindow
                                                                          .
                                                                          Instance
                                                                          .
                                                                          StartStopQAB
                                                                          .
                                                                          IsEnabled
                                                                          =
                                                                          true;
                                                                  }));
                                           }).Start();
                        });
            }
        }

        public static Command RestartServer
        {
            get
            {
                return new Command(
                    () =>
                        {
                            MainWindow.Instance.StopRB.IsEnabled = false;
                            MainWindow.Instance.RestartRB.IsEnabled = false;
                            MainWindow.Instance.Noticon.ContextMenuStrip.Enabled = false;
                            MainWindow.Instance.StartStopQAB.IsEnabled = false;
                            MainWindow.Instance.StopThumb.IsEnabled = false;
                            MainWindow.Instance.RestartThumb.IsEnabled = false;
                            ServerHandler.ProcessHandler.Instance.Restart();
                            new Thread(() =>
                                           {
                                               ServerHandler.ProcessHandler.Instance.
                                                   WaitForExit();

                                               MainWindow.Instance.Noticon.
                                                   ContextMenuStrip.Enabled = true;
                                               MainWindow.Instance.Dispatcher.Invoke(new Action(
                                                   () =>
                                                       {
                                                           MainWindow.Instance.StartStopQAB.
                                                               IsEnabled = true;
                                                       }));
                                           }).Start();
                        });
            }
        }

        public static Command Quit
        {
            get
            {
                return new Command(delegate
                {
                    try
                    {
                        Config.Default.Save();
                    }catch
                    {
                    } try
                    {
                        Config.NetConf.Save();
                    }catch
                    {
                    }
                    try
                    {
                        Config.UserCache.Save();
                    }catch
                    {
                    }

                    Net.Listener.StopListening();

                    if (ServerHandler.ProcessHandler.Instance.IsRunning)
                    {
                        MainWindow.Instance.Dispatcher.Invoke(new Action(() => MainWindow.Instance.Hide()));

                        MainWindow.Instance.Noticon.Icon = null;

                        new Thread(() =>
                        {
                            var sdsbw = new ShuttingDownServerBox();
                            sdsbw.Show();

                            ServerHandler.ProcessHandler.Instance.Stop(true);

                            sdsbw.Close();

                            Environment.Exit(0);
                        }).Start();
                    }
                    else
                        Environment.Exit(0);
                });
            }
        }

        public static Command HideWindow
        {
            get
            {
                return new Command(() => MainWindow.Instance.Hide());
            }
        }

        public static Command ClearConsole
        {
            get
            {
                return new Command(() => MainWindow.Instance.outputBox.Document.Blocks.Clear());
            }
        }

        public static Command UpdateCraftbukkit
        {
            get
            {
                return new Command(
                    () => MainWindow.Instance.Dispatcher.Invoke(new Action(() => new DLWindow().ShowDialog())));
            }
        }

        public static Command SaveConfig
        {
            get
            {
                return new Command(() =>
                {
                    Config.Default["maxheap"] = MainWindow.Instance.memcb.Text;
                                           Config.Default.Save();
                                       });
            }
        }

        public static Command KillServer
        {
            get { return new Command(() => ServerHandler.ProcessHandler.Instance.Kill()); }
        }

        public static Command AddSectionSign
        {
            get
            {
                return new Command(
                    () =>
                        {
                            MainWindow.Instance.inputBox.AppendText("§");
                            MainWindow.Instance.Dispatcher.BeginInvoke(new Action(
                                () =>
                                    {
                                        MainWindow.Instance.inputBox.Select(
                                            MainWindow.Instance.inputBox.Text.Length, 0);

                                        MainWindow.Instance.inputBox.Focus();
                                    }));
                        });
            }
        }

        public static Command ShowPortForward
        {
            get
            {
                return new Command(() =>
                                       {
                                           if (_portForward == null || !_portForward.IsVisible)
                                               _portForward = new PortForward();
                                       });
            }
        }

        public static Command ShowUserManager
        {
            get
            {
                return new Command(() =>
                {
                    if (_userManager == null || !_userManager.IsVisible)
                        _userManager = new UserManager();
                });
            }
        }

        public class Command : ICommand
        {
            private readonly Action _a;
            public event EventHandler CanExecuteChanged;

            public Command(Action a)
            {
                _a = a;
            }

            public void Execute()
            {
                try
                {
                    _a();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("COMMAND_ERROR: " + e);
                }
            }
            public void Execute(object o)
            {
                Execute();
            }
            public void Execute(object sender, EventArgs e)
            {
                Execute();
            }
            public bool CanExecute(object o)
            {
                return true;
            }
        }
    }
}
