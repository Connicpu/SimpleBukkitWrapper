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
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using SBW2.Properties;
using Timer = System.Threading.Timer;

namespace SBW2.ServerHandler
{
    public class ProcessHandler
    {
        #region Publics
        public static readonly ProcessHandler Instance = new ProcessHandler();
        public bool IsRunning
        {
            get
            {
                if (JavaProcess == null) return false;
                try
                {
                    var nstate = JavaProcess.HasExited;
                    return !nstate;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error getting run state: " + e);
                    return false;
                }
            }
        }

        private Process JavaProcess { get; set; }

        public delegate void OutputHandler(object sender, string data);
        public static event OutputHandler Output;

        public static readonly object ConsoleOutput = new object();
        public static readonly object ConsoleError = new object();

        public long WorkingSet
        {
            get
            {
                if (!IsRunning)
                    return 0;
                try
                {
                    JavaProcess.Refresh();
                    return JavaProcess.WorkingSet64;
                }
                catch { return 0; }
            }
        }

        public long Uptime { get; private set; }

        public delegate void StateChangedHandler(bool newstate);
        public event StateChangedHandler StateChanged;
        #endregion

        #region Privates

        private readonly object _lock = new object();
        private readonly ManualResetEventSlim _stopMre;
        #endregion

        private ProcessHandler()
        {
            Uptime = 0;
            _stopMre = new ManualResetEventSlim();

            new Timer(state => { if (IsRunning) ++Uptime; }, null, 1000, 1000);
        }

        private void InitProc()
        {
            JavaProcess = new Process
                           {
                               StartInfo =
                                   {
                                       FileName =
                                           Path.Combine(Helpers.PathHelpers.GetJavaHome(),
                                               "bin\\java.exe"),

                                       RedirectStandardInput = true,
                                       RedirectStandardError = true,
                                       RedirectStandardOutput = true,

                                       Arguments = "-Djline.terminal=jline.UnsupportedTerminal" +
                                                   ((Config.Default["MaxHeap"] != String.Empty)
                                                        ? " -Xmx" + Config.Default["MaxHeap"]
                                                        : " -Xmx1024M") +
                                                   ((Config.Default["MinHeap"] != String.Empty)
                                                        ? " -Xms" + Config.Default["MinHeap"]
                                                        : " -Xms512M") +
                                                   ((Config.Default["Incgc"].Equals("True",
                                                            StringComparison.OrdinalIgnoreCase))
                                                        ? " -Xincgc"
                                                        : "") +
                                                   " -jar " + ResolveJarPath(Config.Default["JarPath"]),

                                       ErrorDialog = true,
                                       UseShellExecute = false,
                                       CreateNoWindow = true,
                                       WorkingDirectory =
                                           Path.Combine(Environment.CurrentDirectory, "server_files")
                                   },
                               EnableRaisingEvents = true
                           };

            JavaProcess.Exited += Exit;
            JavaProcess.OutputDataReceived += (sender, e) => Output(ConsoleOutput, e.Data);
            JavaProcess.ErrorDataReceived += (sender, e) => Output(ConsoleError, e.Data);

            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "server_files")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "server_files"));
            }
        }

        void Exit(object sender, EventArgs e)
        {
            Output(this, "\rServer terminated\r");
            _stopMre.Set();
            StateChangeSafe();
        }

        private void StateChangeSafe()
        {
            try
            {
                StateChanged(IsRunning);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error calling StateChanged: " + e);
            }
        }

        public void Start()
        {
            var wt = new Thread(new ThreadStart(delegate
            {
                lock (_lock)
                {
                    if (IsRunning)
                    {
                        return;
                    }

                    if (!File.Exists(ResolveJarPath(Config.Default["JarPath"])))
                    {
                        var cancel = false;
                        MainWindow.Instance.Dispatcher.Invoke(new Action(delegate
                        {
                            var dr = MessageBox.Show(
                                Resources.ProcessHandler_Start_The_craftbukkit_file_was_not_found__Download_it_,
                                Resources.ProcessHandler_Start_Missing_File,
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Exclamation);
                            if (dr != DialogResult.Yes)
                            {
                                cancel = true;
                            }
                            else new DLWindow().Show();

                        }), new object[0]);

                        if (cancel) return;
                    }

                    InitProc();

                    try
                    {
                        JavaProcess.Start();

                        JavaProcess.BeginOutputReadLine();
                        JavaProcess.BeginErrorReadLine();

                        _stopMre.Reset();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(
                            Resources.ProcessHandler_Start_Error_starting_server__ + e.Message);

                    }

                    StateChangeSafe();
                }
            }));
            wt.Start();
        }

        public void Stop(bool wait = false)
        {
            Command("save-all");
            Command("stop");

            if (wait)
            {
                WaitForExit();
            }
        }

        public void Restart(bool wait = false)
        {
            lock (_lock)
            {
                if (wait)
                {
                    Stop(true);
                    Start();
                }
                else
                {
                    new Thread(() =>
                                   {
                                       Stop(true);
                                       Start();
                                   }).Start();
                }
            }
        }

        public void Command(string cmd, bool echo = false)
        {
            if (echo)
            {
                try
                {
                    Output(this, "command> " + cmd + '\r');
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error on command echo: " + e);
                }
            }
            try
            {
                JavaProcess.StandardInput.Write(cmd + '\n');
            }
            catch (NullReferenceException)
            {
                ExtOutput(Resources.ProcessHandler_Command_Server_is_not_running);
            }
            catch (IOException ex)
            {
                Debug.WriteLine("Error writing to Java StdIn: " + ex.Message);
            }
        }

        public static void ExtOutput(string data)
        {
            try
            {
                Output(null, data);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error on ExtOutput: " + e);
            }
        }

        public static string ResolveJarPath(string path)
        {
            string newpath = path;
            newpath = newpath.Replace("%ad%", Environment.CurrentDirectory);
            newpath = newpath.Replace("\\\\", "\\");
            return newpath;
        }

        public void WaitForExit()
        {
            _stopMre.Wait();
        }

        public void Kill()
        {
            JavaProcess.Kill();
        }
    }
}
