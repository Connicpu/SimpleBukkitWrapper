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
using System.Windows.Forms;
using System.Net;
using System.IO;
using SBW2.Properties;

namespace SBW2
{
    public partial class DLWindow : Form
    {
        public static readonly string CBUri = Config.Default["cburi"];

        private readonly WebClient _wc;

        public DLWindow()
        {
            Application.EnableVisualStyles();
            _wc = new WebClient();
            _wc.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs e)
            {
                var nlt = e.BytesReceived / 1024 + "KB";
                var npbv = 0;
                ProgressBarStyle npbs;

                if (e.TotalBytesToReceive <= 0)
                {
                    nlt += " / ?";
                    npbs = ProgressBarStyle.Marquee;
                }
                else
                {
                    nlt += " / " + e.TotalBytesToReceive / 1024 + "KB - " + e.ProgressPercentage + "%";
                    npbs = ProgressBarStyle.Blocks;
                    npbv = e.ProgressPercentage;
                }
                try
                {
                    Invoke(new Action(delegate
                    {
                        label1.Text = nlt;
                        progressBar1.Style = npbs;
                        progressBar1.Value = npbv;
                    }));
                }
                catch { }
            };

            _wc.DownloadFileCompleted += delegate(object sender, AsyncCompletedEventArgs e)
            {
                if (e.Cancelled)
                {
                    return;
                }
                if (e.Error != null)
                {
                    MessageBox.Show("Error!\n" + e.Error.Message);
                    return;
                }

                Ionic.Zip.ZipFile zf;
                try
                {
                    zf = new Ionic.Zip.ZipFile(ServerHandler.ProcessHandler.ResolveJarPath("%ad%\\cb_artifact.zip"));
                }
                catch (Exception ex) { MessageBox.Show(Resources.DLWindow_DLWindow_ + ex.Message); return; }
                BeginInvoke(new Action(delegate
                {
                    try
                    {
                        foreach (var zfe in zf)
                        {
                            if (!zfe.IsDirectory)
                            {
                                FileStream fs;
                                zfe.Extract(fs = File.Open(ServerHandler.ProcessHandler.ResolveJarPath(Config.Default["jarpath"]),
                                    FileMode.OpenOrCreate, FileAccess.Write));
                                fs.Close();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Resources.DLWindow_DLWindow_ + ex.Message);
                    }
                    this.Close();

                }));
            };

            InitializeComponent();
        }

        private void DlWindowLoad(object sender, EventArgs e)
        {

        }

        private void Button1Click(object sender, EventArgs e)
        {
            new System.Threading.Thread(new System.Threading.ThreadStart(delegate
            {
                try
                {
                    Invoke(() => { ControlBox = false; });

                    if (File.Exists(ServerHandler.ProcessHandler.ResolveJarPath(Config.Default["jarpath"])))
                    {
                        File.Delete(ServerHandler.ProcessHandler.ResolveJarPath(Config.Default["jarpath"]));
                    }

                    _wc.DownloadFileAsync(new Uri(CBUri), ServerHandler.ProcessHandler.ResolveJarPath("%ad%\\cb_artifact.zip"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Resources.DLWindow_button1_Click_Error_downloading_file__ + ex.Message);

                    Invoke(Close);
                }
            })).Start();

            label1.Text = Resources.DLWindow_button1_Click_Connecting___;

            button1.Enabled = false;
        }

        private void DlWindowFormClosing(object sender, FormClosingEventArgs e)
        {
            _wc.CancelAsync();
        }

        private void Invoke(Action a)
        {
            Invoke((Delegate) a);
        }
    }
}
