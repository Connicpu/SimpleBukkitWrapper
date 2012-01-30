using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SBW2.Net
{
    internal class Client
    {
        private Stream c;
        private readonly Thread _clientThread;
        private int _mode;
        private int sec;
        private string user;
        private Encoding e = Encoding.UTF8;
        private readonly byte[] r = new byte[4096];

        public Client(TcpClient client)
        {
            c = client.GetStream();

            (_clientThread = new Thread(Negotiate)).Start();
        }

        private void Negotiate()
        {
            bool t;

            write("Which encoding will you use?\r\n");

            var encoding = read(out t);
            if (t) return;

            switch (encoding.ToLower())
            {
                case "utf-16":
                    e = Encoding.Unicode;
                    break;
                case "unicode":
                    goto case "utf-16";
                case "utf-8":
                    e = Encoding.UTF8;
                    break;
                case "ascii":
                    e = Encoding.ASCII;
                    break;
                case "utf-7":
                    e = Encoding.UTF7;
                    break;
                case "utf-32":
                    e = Encoding.UTF32;
                    break;
                case "bigendianunicode":
                    e = Encoding.BigEndianUnicode;
                    break;
                default:
                    goto case "utf-8";
            }

            write("Which mode? Hint: If you are human, you probably want basic\r\n");
            var mode = read(out t);
            if (t) return;

            switch (mode.ToLower())
            {
                case "basic":
                    _mode = 0;
                    break;
                case "advanced":
                    _mode = 1;
                    break;
                case "advanced_ssl":
                    _mode = 1;
                    SslNegotiation();
                    break;
                default:
                    goto case "basic";
            }

            write("Username?\r\n");

            user = read(out t);
            if (t) return;

            write("Password?\r\n");

            var pass = read(out t);
            if (t) return;

            sec = AuthProvider.Authenticate(user, pass, c, e);

            if (sec == -1)
            {
                write("\u001B[31mThe username or password was incorrect\u001B[0m\r\n");
                c.Close();
                return;
            }

            write("\u001B[32mYou have been authenticated with a clearance level of " + sec + "\u001B[0m\r\n");

            switch (_mode)
            {
                case 0:
                    BasicLoop();
                    break;
                case 1:
                    AdvancedLoop();
                    break;
            }
        }

        private void RecieveData(object sender, string data)
        {
            try
            {
                write(data + "\r\n");
            }
            catch
            {
            }
        }

        private void BasicLoop()
        {
            try
            {
                ServerHandler.ProcessHandler.Output += RecieveData;

                while (true)
                {
                    bool t;
                    var command = read(out t);
                    if (t) return;
                    if (command.StartsWith("#memory"))
                    {
                        write("\u001B[33mMemory: " + ServerHandler.ProcessHandler.Instance.WorkingSet / 1024 / 1024 + " MB\u001B[0m\r\n");
                    }
                    CommandHandlers.ProcessCommand(command, c, sec, user, e);
                }
            }
            finally
            {
                ServerHandler.ProcessHandler.Output -= RecieveData;
            }
        }

        private static void AdvancedLoop()
        {

        }

        private void SslNegotiation()
        {

            var cert = AuthProvider.GetCert();
            if (cert == null)
            {
                write("NO_SSL");
                return;
            }

            write("AUTH_SSL");

            var ssl = new SslStream(c, false);
            ssl.AuthenticateAsServer(cert);
            c = ssl;
        }

        public void Abort()
        {
            ServerHandler.ProcessHandler.Output -= RecieveData;
            _clientThread.Abort();
        }

        private void write(string s)
        {
            var a = e.GetBytes(s);
            c.Write(a, 0, a.Length);
        }

        private string read(out bool t)
        {
            try
            {
            start:
                t = false;
                var b = c.Read(r, 0, 4096);

                if (b == 0)
                {
                    t = true;
                    return "";
                }
                var result = e.GetString(r, 0, b);
                if (String.IsNullOrWhiteSpace(result))
                {
                    goto start;
                }

                return result;
            }
            catch (IOException) { t = true; return ""; }
        }
    }
}