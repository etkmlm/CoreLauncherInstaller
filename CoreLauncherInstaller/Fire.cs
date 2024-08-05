using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace CoreLauncherInstaller
{
    internal class Fire
    {
        public static readonly string LATEST = "https://laeben-update-default-rtdb.europe-west1.firebasedatabase.app/apps/clauncher/latest.json";
        public static readonly string GET_VERSIONS = "https://laeben-update-default-rtdb.europe-west1.firebasedatabase.app/apps/clauncher/files.json";
        public static readonly string WRAPPER = "https://laeben-update-default-rtdb.europe-west1.firebasedatabase.app/apps/clauncher/wrapper.json";

        public static List<File> GetFiles()
        {
            using (var client = new WebClient())
            {
                var str = client.DownloadString(GET_VERSIONS);
                return JsonConvert.DeserializeObject<List<File>>(str);
            }
        }

        private static double GetLatest()
        {
            using (var client = new WebClient())
            {
                var str = client.DownloadString(LATEST);
                return double.Parse(str);
            }
        }

        public static string GetLatestWrapper()
        {
            using (var client = new WebClient())
            {
                return client.DownloadString(WRAPPER).Replace("\"", "");
            }
        }

        public static File GetLatestFile()
        {
            var files = GetFiles();
            var latest = GetLatest();

            return files.FirstOrDefault(a => a.Version == latest);
        }
    }
}
