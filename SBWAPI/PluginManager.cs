using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SBWAPI
{
    public class PluginManager
    {
        public delegate bool CommandProcessor(string cmd, Stream io, int security, string user, Encoding encoding);

        private readonly Dictionary<String, CommandProcessor> _commands = new Dictionary<string,CommandProcessor>(); 

        public static PluginManager Default { get; private set; }

        static PluginManager()
        {
            Default = new PluginManager();
        }

        public bool RegisterCommand(string label, CommandProcessor callback)
        {
            if (_commands.ContainsKey(label)) return false;
            _commands[label] = callback;
            return true;
        }

        public bool ProcessCommand(string label, string command, Stream io, int security, string user, Encoding encoding)
        {
            return _commands.ContainsKey(label) && _commands[label](command, io, security, user, encoding);
        }
    }
}
