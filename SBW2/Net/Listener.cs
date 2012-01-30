using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SBW2.Net
{
    internal static class Listener
    {
        private static TcpListener _listener;
        private static Thread _listenThread;
        private static Thread _flashThread;
        private static int _recentIoEs;
        private static readonly List<Client> Clients = new List<Client>();

        internal static void StartListening(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);

            _listenThread = new Thread(() =>
                                           {
                                               _listener.Start();

                                               while (true)
                                               {
                                                   try
                                                   {
                                                       if (_recentIoEs > 100)
                                                       {
                                                           _recentIoEs = 0;
                                                           StartListening(port);
                                                           return;
                                                       }
                                                       var client = _listener.AcceptTcpClient();
                                                       Clients.Add(new Client(client));
                                                   }
                                                   catch (ThreadAbortException)
                                                   {
                                                       break;
                                                   }
                                                   catch (SocketException)
                                                   {
                                                       break;
                                                   }
                                                   catch (IOException)
                                                   {
                                                       ++_recentIoEs;
                                                   }
                                               }
                                           });
            _listenThread.Start();
            ListenFlash();
        }

        private static void ListenFlash()
        {
            (_flashThread = new Thread(
                                () =>
                                    {
                                        try
                                        {
                                            var tcp = new TcpListener(IPAddress.Any, 843);
                                            tcp.Start();
                                            while (true)
                                            {
                                                var c = tcp.AcceptTcpClient();
                                                try
                                                {
                                                    var buffer = new byte[4096];
                                                    c.GetStream().Read(buffer, 0, 4096);
                                                    buffer = Encoding.UTF8.GetBytes(Properties.Resources.FlashAuthData +
                                                                                    '\u0000');
                                                    c.GetStream().Write(buffer, 0, buffer.Length);
                                                    c.Close();
                                                }
                                                catch (ThreadAbortException)
                                                {
                                                    throw;
                                                }
                                                catch
                                                {
                                                    Debug.WriteLine("Flash auther client crashed; Listener still going;");
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            Debug.WriteLine("Flash auther crashed;");
                                        }
                                    })).Start();
        }

        internal static void StopListening()
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error stopping listener: " + e);
            }
            try
            {
                _listenThread.Abort();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error stopping listener thread: " + e);
            }
            foreach (var c in Clients)
                try
                {
                    c.Abort();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error stopping a client: " + e);
                }
            try
            {
                _flashThread.Abort();
            } catch { }
        }
    }
}