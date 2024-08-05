using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CoreLauncherWrapper
{
    internal class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern void SetStdHandle(int nStdHandle, IntPtr handle);

        static void ShowConsole()
        {
            if (!AttachConsole(-1))
                AllocConsole();
        }

        static void Main(string[] args)
        {
            if (Java.TryCheck(out var check))
            {
                var ver = check?.FirstOrDefault(x => x.MajorVersion >= 17);
                if (ver != null)
                {
                    Launch(ver?.Path);
                    Console.Read();
                    return;
                }
            }

            ShowConsole();

            Console.WriteLine("-- You are seeing this because you have not Java >= 17 :) --");
            Console.WriteLine("Trying to install Java 17...");

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.DefaultConnectionLimit = 999999;

            try 
            {
                Java.Install(Path.Combine(Environment.CurrentDirectory, "Java"), a => Console.Write("\r%" + a), a =>
                {
                    if (!a)
                    {
                        Console.WriteLine("Error while installing Java 17, try to install manually.");
                        Console.ReadKey();
                    }
                    else
                        Main(args);
                });
            }
            catch(Exception e)
            {
                Console.WriteLine("Error (probably no connection): " + e.Message);
            }
            Console.ReadKey();
        }

        static void Launch(string java)
        {
            var launcher = Path.Combine(Environment.CurrentDirectory, "CoreLauncher.jar");

            if (!File.Exists(launcher))
            {
                ShowConsole();
                Console.WriteLine($"-- The launcher file couldn't find: {launcher} --");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Path.Combine(java, "bin\\java.exe"),
                Arguments = "-jar CoreLauncher.jar",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Environment.Exit(0);
        }
    }
}
