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
using System.Collections.Generic;
using System.IO;

namespace SBW2
{
    public class Config
    {
        public static Config Default { get; private set; }
        public static Config NetConf { get; private set; }
        public static Config UserCache { get; private set; }

        private readonly Dictionary<string, string> _data = new Dictionary<string,string>();
        private readonly string _path;

        static Config()
        {
            try
            {
                if (!File.Exists("config.txt"))
                {
                    using (var sw = new StreamWriter("config.txt"))
                    {
                        sw.Write(Properties.Resources.DefaultConfig);
                    }
                }
                Default = new Config("config.txt");
            }
            catch
            {
                Default = new Config("", "config.txt");
            }

            try
            {
                if (!File.Exists("net-conf.txt"))
                {
                    using (var sw = new StreamWriter("net-conf.txt"))
                    {
                        sw.Write(Properties.Resources.DefaultNetConfig);
                    }
                }
                NetConf = new Config("net-conf.txt");
            }
            catch
            {
                NetConf = new Config("", "net-conf.txt");
            }

            try
            {
                if (!File.Exists("user-cache.txt"))
                    using (var sw = new StreamWriter("user-cache.txt"))
                    {
                        sw.Write("");
                    }
                UserCache = new Config("user-cache.txt");
            }
            catch
            {
                UserCache = new Config("", "user-cache.txt");
            }

        }

        private Config(string path) :this(null, path)
        {
            _path = path;
        }

        private Config(string data, string spath)
        {
            if (data == null)
            {
                data = File.ReadAllText(spath);
            }

            using (var sr = new StringReader(data))
            {
                string line;
                while (!String.IsNullOrWhiteSpace(line = sr.ReadLine()))
                {
                    if (String.IsNullOrWhiteSpace(line)) continue;
                    var split = line.Split(new[] { '=' }, 2);
                    if (split.Length < 2) continue;

                    _data[split[0].ToLower().Trim()] = split[1].Trim();
                }
            }

            _path = spath;
        }

        public string this[string key]
        {
            get
            {
                return !_data.ContainsKey(key.ToLower()) ? String.Empty : _data[key.ToLower()];
            }
            set
            {
                _data[key.ToLower()] = value;
            }
        }

        /// <summary>
        /// Saves all the data contained in the config to disc;
        /// </summary>
        public void Save()
        {
            using (var sw = new StreamWriter(_path))
            {
                foreach (var kvp in _data)
                {
                    sw.WriteLine(kvp.Key + '=' + kvp.Value);
                }
                sw.Close();
            }
        }

        /// <summary>
        /// Reloads the config data from the file it was originally loaded from
        /// </summary>
        public void Reload()
        {
            var newdat = new Config(_path)._data;

            foreach (var kvp in newdat)
            {
                _data[kvp.Key] = kvp.Value;
            }
        }

        public List<String> Keys
        {
            get { return new List<String>(_data.Keys); }
        }
    }
}
