using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SBW2.Net
{
    public static class CommandHandlers
    {
        private delegate void CommandProcessor(string command, Stream io, int security, string user, Encoding ioencoding);

        public static void ProcessCommand(string command, Stream io, int security, string user, Encoding encoding)
        {
            if (String.IsNullOrWhiteSpace(command))
            {
                return;
            }

            try
            {
                byte[] writebuffer;

                var cmdsplit = command.Split(' ');
                if (cmdsplit.Length < 1) return;
                var label = cmdsplit[0];

                if (SBWAPI.PluginManager.Default.ProcessCommand(label, command, io, security, user, encoding)) return;

                if (command.StartsWith("#"))
                {
                    if (security < 4)
                    {
                        writebuffer =
                            encoding.GetBytes(
                                "\u001B[31mYou do not have permission to send special commands\u001B[0m\r\n");
                        io.Write(writebuffer, 0, writebuffer.Length);
                        return;
                    }

                    if (CommandProcessors.ContainsKey(label))
                        CommandProcessors[label](command, io, security, user, encoding);
                    return;
                }
                if (command.StartsWith("/"))
                {
                    if (security < 3)
                    {
                        writebuffer = encoding.GetBytes(
                            "\u001B[31mYou do not have permission to send commands\u001B[0m\r\n");
                        io.Write(writebuffer, 0, writebuffer.Length);
                        return;
                    }

                    if (CommandProcessors.ContainsKey(label))
                        CommandProcessors[label](command, io, security, user, encoding);
                    else
                    {
                        ServerHandler.ProcessHandler.ExtOutput(string.Format("<{0}> {1}", user, command));
                        ServerHandler.ProcessHandler.Instance.Command(command.Remove(0, 1));
                    }
                    return;
                }

                CommandProcessors["\uFFFF"](command, io, security, user, encoding);
            }
            catch
            {
            }
        }

        private static readonly Dictionary<String, CommandProcessor> CommandProcessors =
            new Dictionary<string, CommandProcessor>();

        static CommandHandlers()
        {
            CommandProcessors.Add(
                "\uFFFF",
                (command, io, security, user, encoding) =>
                ServerHandler.ProcessHandler.Instance.Command(
                    string.Format(Config.NetConf["TalkFormat"], user, command)));
            CommandProcessors.Add(
                "/start",
                (command, io, security, user, encoding) =>
                    {
                        if (security < 4)
                        {
                            var b =
                                encoding.GetBytes(
                                    "\u001B[31mYou do not have the clearance to perform this command\u001B[0m\r\n");
                            io.Write(b, 0, b.Length);
                            return;
                        }
                        if (ServerHandler.ProcessHandler.Instance.IsRunning)
                        {
                            var b = encoding.GetBytes("\u001B[31mThe server is already running\u001B0m\r\n");
                            io.Write(b, 0, b.Length);
                            return;
                        }

                        var c = encoding.GetBytes("\u001B[32m" + user + " has attempted to start the server\u001B[0m\r\n");
                        io.Write(c, 0, c.Length);
                        Commands.StartServer.Execute();
                    });
            CommandProcessors.Add(
                "/stop",
                (command, io, security, user, encoding) =>
                    {
                        if (security < 4)
                        {
                            var a = encoding.GetBytes("\u001B[31mYou do not have the clearance to perform this command\u001B[0m\r\n");
                            io.Write(a, 0, a.Length);
                            return;
                        }
                        if (!ServerHandler.ProcessHandler.Instance.IsRunning)
                        {
                            var b =
                                encoding.GetBytes("\u001B[31mThe server is not running and cannot be stopped\u001B\r\n");
                            io.Write(b, 0, b.Length);
                            return;
                        }

                        var c = encoding.GetBytes("\u001B[31m" + user + " has attempted to stop the server\u001B[0m\r\n");
                        io.Write(c, 0, c.Length);
                        Commands.StopServer.Execute();
                    });
            CommandProcessors.Add(
                "/restart",
                (command, io, security, user, encoding) =>
                    {
                        if (security < 4)
                        {
                            var a =
                                encoding.GetBytes(
                                    "\u001B[31mYou do not have the clearance to perform this command\u001B[0m\r\n");
                            io.Write(a, 0, a.Length);
                            return;
                        }
                        if (!ServerHandler.ProcessHandler.Instance.IsRunning)
                        {
                            var b =
                                encoding.GetBytes(
                                    "\u001B[31mThe server is not running and cannot be stopped\u001B[0m\r\n");
                            io.Write(b, 0, b.Length);
                            return;
                        }

                        var c = encoding.GetBytes("\u001B[33m" + user + " has attemped to restart the server");
                        io.Write(c, 0, c.Length);
                        Commands.RestartServer.Execute();
                    });
        }
    }
}
