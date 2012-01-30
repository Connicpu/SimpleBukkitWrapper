using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;

namespace SBWAPI
{
    public class Plugin
    {
        public string Friendlyname { get; private set; }
        public string Entry { get; private set; }
        public string Libname { get; private set; }
        public string Weburl { get; private set; }
        public string Author { get; private set; }
        public string Description { get; private set; }

        private Plugin()
        {
            Friendlyname = "";
            Entry = "";
            Libname = "";
            Weburl = "";
            Author = "";
            Description = "";
        }

        public static Plugin FromPluginFile(string path)
        {
            var result = new Plugin();

            var data = File.ReadAllLines(path);
            if (data.Length < 3) return null;

            result.Friendlyname = data[0];
            result.Entry = data[1];
            result.Libname = data[2];
            if (data.Length > 3) result.Weburl = data[3];
            if (data.Length > 4) result.Author = data[4];
            if (data.Length > 5)
            {
                for (var i = 5; i < data.Length; ++i)
                {
                    if (i > 5) result.Description += Environment.NewLine;
                    result.Description += data[i];
                }
            }

            return result;
        }

        public void LoadPlugin()
        {
            try
            {
                if (!File.Exists("plugins\\" + Libname))
                {
                    if (!string.IsNullOrEmpty(Weburl))
                    {
                        new WebClient().DownloadFile(Weburl, "plugins\\" + Libname);
                    }
                    else return;
                }
                var plugasm = Assembly.LoadFrom("plugins\\" + Libname);
                var plugin = (IPlugin) plugasm.CreateInstance(Entry);

                if (plugin != null) plugin.Load(PluginManager.Default);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Plugin load error: " + ToString() + e);
            }
        }

        public override string ToString()
        {
            var result = Friendlyname + ", by " + Author + Environment.NewLine;
            result += "Entry Class: " + Entry + Environment.NewLine;
            result += "Description: " + Description + Environment.NewLine;

            return result;
        }
    }
}
