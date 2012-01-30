using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Linq;

namespace SBW2.Net
{
    public static class AuthProvider
    {
        static readonly SHA1 Sha1 = new SHA1CryptoServiceProvider();

        public static int Authenticate(string user, string pass, Stream status, Encoding encoding)
        {
            var passhash = Sha1Hash(pass);
            byte[] b;

            if (Config.UserCache[user + "§custom"] == "true")
            {
                
                b = encoding.GetBytes("\u001B[36mAuthenticating against local hash...\u001B[0m\r\n");
                status.Write(b, 0, b.Length);

                if (passhash != Config.UserCache[user + "§hash"])
                {
                    return -1;
                }

                int seclvlc;
                var prc = int.TryParse(Config.UserCache[user], out seclvlc);
                return prc ? seclvlc : 0;
            }

            b = encoding.GetBytes("\u001B[36mAuthenticating with minecraft.net\r\n");
            status.Write(b, 0, b.Length);

            var olresult = ValidateOnline(user, pass);

            if (olresult == null)
            {
                b=encoding.GetBytes("\u001B[31mError connecting to minecraft.net\r\n");
                status.Write(b, 0, b.Length);
                b = encoding.GetBytes("\u001B[36mAuthenticating against local cache...\u001B[0m\r\n");
                status.Write(b, 0, b.Length);
                if (Config.UserCache[user + "§hash"] != "")
                {
                    if (passhash == Config.UserCache[user + "§hash"])
                    {
                        olresult = true;
                    }
                    else return -1;
                }
                else
                {
                    b = encoding.GetBytes("\u001B[31mPassword not availible in cache\u001B[0m\r\n");
                    status.Write(b, 0, b.Length);
                    return -1;
                }
            }

            if (olresult == false) return -1;

            Config.UserCache[user + "§hash"] = passhash;

            int seclvl;
            var pr = int.TryParse(Config.UserCache[user], out seclvl);
            return pr ? seclvl : 0;
        }

        public static int Authenticate(string user, string pass)
        {
            if (Config.UserCache[user + "§custom"] == "true")
            {
                int seclvlc;
                var prc = int.TryParse(Config.UserCache[user], out seclvlc);
                return prc ? seclvlc : 0;
            }

            var olresult = ValidateOnline(user, pass);
            var passhash = Sha1Hash(pass);

            if (olresult == null)
            {
                if (Config.UserCache[user + "§hash"] != "")
                {
                    if (passhash == Config.UserCache[user + "§hash"])
                    {
                        olresult = true;
                    }
                    else return -1;
                }
                else return -1;
            }

            if (olresult == false) return -1;

            Config.UserCache[user + "§hash"] = passhash;

            int seclvl;
            var pr = int.TryParse(Config.UserCache[user], out seclvl);
            return pr ? seclvl : 0;
        }

        public static X509Certificate GetCert()
        {
            if (Config.Default["certificate"] == "" || Config.Default["certificate"] == "NO_CERT")
            {
                return null;
            }

            return String.IsNullOrWhiteSpace(Config.Default["cert_pass"]) ? new X509Certificate(Config.Default["certificate"]) :
                new X509Certificate(Config.Default["certificate"], Config.Default["cert_pass"]);
        }

        static bool? ValidateOnline(string user, string pass)
        {
            var result = "";
            try
            {
                var parameters = "user=" + HttpUtility.UrlEncode(user) + "&password=" + HttpUtility.UrlEncode(pass) + "&version=0";
                const string url = "https://login.minecraft.net/";

                var request = WebRequest.Create(url);

                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                var bytes = Encoding.UTF8.GetBytes(parameters);
                Stream os = null;
                try
                {
                    request.ContentLength = bytes.Length;
                    os = request.GetRequestStream();
                    os.Write(bytes, 0, bytes.Length);
                    os.Flush();
                }
                finally
                {
                    if (os != null)
                    {
                        os.Close();
                    }
                }
                var response = request.GetResponse();
                var responseStream = response.GetResponseStream();
                if (responseStream != null)
                {
                    var sr = new StreamReader(responseStream);
                    result = sr.ReadToEnd().Trim();
                    sr.Close();
                }
            }
            catch
            {
                return null;
            }
            return result == "Old version";
        }

        public static string Sha1Hash(string input)
        {
            var a = Encoding.UTF8.GetBytes(input);
            var b = Sha1.ComputeHash(a);
            return b.Aggregate("", (current, b1) => current + b1.ToString("x2"));
        }
    }
}