using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreLauncherInstaller
{
    internal static class Fire
    {
        public static readonly string LATEST = "https://laeben-update-default-rtdb.europe-west1.firebasedatabase.app/apps/clauncher/latest.json";
        public static readonly string GET_VERSIONS = "https://laeben-update-default-rtdb.europe-west1.firebasedatabase.app/apps/clauncher/files.json";
        public static readonly string WRAPPER = "https://laeben-update-default-rtdb.europe-west1.firebasedatabase.app/apps/clauncher/wrapper.json";

        private static readonly HttpClient client = new HttpClient
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0" }
            }
        };

        public static async Task<List<File>> GetFiles()
        {
            var str = await client.GetStringAsync(GET_VERSIONS);
            return JsonConvert.DeserializeObject<List<File>>(str);
        }

        private static async Task<double> GetLatest()
        {
            var str = await client.GetStringAsync(LATEST);
            return double.Parse(str, CultureInfo.GetCultureInfo("en_US"));
        }

        public static async Task<string> GetLatestWrapper()
        {
            return (await client.GetStringAsync(WRAPPER)).Replace("\"", "");
        }

        public static async Task<File> GetLatestFile()
        {
            var files = await GetFiles();
            var latest = await GetLatest();

            return files.FirstOrDefault(a => a.Version == latest);
        }
    }
}
