using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace CoreLauncherWrapper
{
    internal class Java
    {
        [DllImport("kernel32.dll")]
        private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

        public struct JavaVersion
        {
            public int MajorVersion;
            public string Path;
            public bool IsJDK;
        }
        public static IEnumerable<JavaVersion> Check()
        {
            var reg = Registry.LocalMachine;
            var jre = reg.OpenSubKey(@"SOFTWARE\JavaSoft\Java Runtime Environment");
            var jdk = reg.OpenSubKey(@"SOFTWARE\JavaSoft\JDK");

            var eclipse = Path.Combine(Environment.CurrentDirectory, "Java");

            if (Directory.Exists(eclipse) && File.Exists(Path.Combine(eclipse, "bin\\java.exe")))
            {
                yield return new JavaVersion
                {
                    IsJDK = true,
                    MajorVersion = 17,
                    Path = eclipse
                };
            }
            else if (jre != null)
            {
                //var ver = jre.GetValue("CurrentVersion").ToString().Split('.')[0];

                //return null;
            }
            else if (jdk != null)
            {
                foreach(var v in jdk.GetSubKeyNames())
                {
                    var vjdk = jdk.OpenSubKey(v);
                    if (!int.TryParse(v.Split('.')[0], out int ver))
                        continue;
                    var path = vjdk.GetValue("JavaHome").ToString();

                    yield return new JavaVersion
                    {
                        IsJDK = true,
                        MajorVersion = ver,
                        Path = path
                    };
                }
                

            }
            /*else
                return null;*/
        }

        public static bool TryCheck(out IEnumerable<JavaVersion> versions)
        {
            try
            {
                versions = Check();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error while checking up Java: " + ex.Message);
                
            }

            versions = null;
            return false;
        }

        private static bool Is64Bit()
        {
            bool x = IsWow64Process2(Process.GetCurrentProcess().Handle, out ushort a, out ushort b);

            bool amd64 = ((int)ImageFileMachine.AMD64 & b) == (int)ImageFileMachine.AMD64; ;
            bool intel64 = ((int)ImageFileMachine.IA64 & b) == (int)ImageFileMachine.IA64;

            return x && (amd64 || intel64);
        }

        /*public static void Install(string path, Action<int> progress, Action<bool> done)
        {
            bool bit64 = Is64Bit();
            string url = "https://api.adoptium.net/v3/assets/latest/17/hotspot?image_type=jdk&os=windows&architecture=" + (bit64 ? "x64" : "x86");

            using(var client = new WebClient())
            {
                client.DownloadProgressChanged += (a, b) => progress(b.ProgressPercentage);

                string json = client.DownloadString(url);
                var match = Regex.Match(json, ".*installer\":[^{]*{.*?\"link\":[^\"]\"([^\"]*)\".*}.*", RegexOptions.Singleline);
                if (!match.Success)
                {
                    done(false);
                    return;
                }

                var jdkLink = match.Groups[1].Value;

                string msiPath = Path.Combine(Environment.CurrentDirectory, "java.msi");

                client.DownloadFileCompleted += (a, b) =>
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = "msiexec.exe",
                        Arguments = $"/i java.msi INSTALLDIR=\"{path}\" /qb",
                        WorkingDirectory = Environment.CurrentDirectory
                    };
                    Process.Start(info).WaitForExit();
                    File.Delete(msiPath);
                    done(true);
                };
                
                client.DownloadFileAsync(new Uri(jdkLink), msiPath);
            }
        }*/

        public static void Install(string path, Action<int> progress, Action<bool> done)
        {
            bool bit64 = Is64Bit();
            string url = "https://api.adoptium.net/v3/assets/latest/17/hotspot?image_type=jdk&os=windows&architecture=" + (bit64 ? "x64" : "x86");

            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += (a, b) => progress(b.ProgressPercentage);

                string json = client.DownloadString(url);
                var match = Regex.Match(json, ".*package\":[^{]*{.*?\"link\":[^\"]\"([^\"]*)\".*}.*", RegexOptions.Singleline);
                if (!match.Success)
                {
                    done(false);
                    return;
                }

                var jdkLink = match.Groups[1].Value;

                string archivePath = Path.Combine(Environment.CurrentDirectory, "java.zip");

                if (File.Exists(archivePath))
                {
                    Console.WriteLine();
                    Console.WriteLine("Unzipping existing Java...");
                    Done(archivePath, done);
                    return;
                }

                client.DownloadFileCompleted += (a, b) =>
                {
                    Console.WriteLine();
                    Console.WriteLine("Unzipping Java...");
                    Done(archivePath, done);
                };

                client.DownloadFileAsync(new Uri(jdkLink), archivePath);
            }
        }

        private static void Done(string archive, Action<bool> done)
        {
            var f = Path.Combine(Environment.CurrentDirectory, "Java");
            var f3 = Path.Combine(Environment.CurrentDirectory, "Java1");
            //Directory.CreateDirectory(f);
            using (var file = ZipFile.OpenRead(archive))
            {
                file.ExtractToDirectory(f, true);
                //file.ExtractAll(f, ExtractExistingFileAction.OverwriteSilently);
            }

            var f2 = Directory.GetDirectories(f)[0];
            Directory.Move(f2, f3);
            Directory.Delete(f);
            Directory.Move(f3, f);

            File.Delete(archive);
            done(true);
        }
    }

    public static class ZipExt
    {
        public static void ExtractToDirectory(this ZipArchive archive, string dir, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(dir);
                return;
            }

            Directory.CreateDirectory(dir);

            foreach(var entry in archive.Entries)
            {
                string path = Path.GetFullPath(Path.Combine(dir, entry.FullName));
                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    continue;
                }
                entry.ExtractToFile(path, true);
            }
        }
    }
}
