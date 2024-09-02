using IWshRuntimeLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


using FolderDialog = System.Windows.Forms.FolderBrowserDialog;

namespace CoreLauncherInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string path;

        private readonly WshShell shell;

        public MainWindow()
        {
            InitializeComponent();

            shell = new WshShell();

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.DefaultConnectionLimit = 999999;

            btnClose.Click += (a, b) => Environment.Exit(0);
            txtPath.TextChanged += (a, b) => path = txtPath.Text;
            txtPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            txtPath.PreviewMouseLeftButtonDown += (a, b) =>
            {
                var dialog = new FolderDialog
                {
                    SelectedPath = path
                };
                var f = dialog.ShowDialog();
                if (f != System.Windows.Forms.DialogResult.OK)
                    return;

                txtPath.Text = dialog.SelectedPath;
            };
            btnInstall.Click += (a, b) => new Thread(() =>
            {
                try
                {
                    var newest = Fire.GetLatestFile().GetAwaiter().GetResult();

                    if (newest == null)
                    {
                        Dispatcher.Invoke(new Action(() => MessageBox.Show(Properties.Resources.ErrorDesc + Properties.Resources.ErrorNewVersion, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error)));
                        return;
                    }

                    string xPath = Path.Combine(path, "Core Launcher");
                    string jarPath = Path.Combine(xPath, "CoreLauncher.jar");
                    string exePath = Path.Combine(xPath, "CoreLauncher.exe");

                    if (System.IO.File.Exists(jarPath))
                    {
                        Dispatcher.Invoke(new Action(() => MessageBox.Show(Properties.Resources.AlreadyInstalled, Properties.Resources.Oops, MessageBoxButton.OK, MessageBoxImage.Error)));
                        return;
                    }

                    Directory.CreateDirectory(xPath);
                    DownloadFileAsync(newest.URL, jarPath, x => Dispatcher.Invoke(new Action(() => progress.Value = x))).GetAwaiter().GetResult();

                    Dispatcher.Invoke(new Action(() =>
                    {
                        if (chkDesktop.IsChecked ?? false)
                        {
                            var lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Core Launcher.lnk");
                            IWshShortcut cut = (IWshShortcut)shell.CreateShortcut(lnk);
                            cut.TargetPath = exePath;
                            cut.WorkingDirectory = xPath;
                            cut.Description = "Core Launcher";
                            cut.Save();
                        }

                        if (chkStart.IsChecked ?? false)
                        {
                            var lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Core Launcher.lnk");
                            IWshShortcut cut = (IWshShortcut)shell.CreateShortcut(lnk);
                            cut.TargetPath = exePath;
                            cut.WorkingDirectory = xPath;
                            cut.Description = "Core Launcher";
                            cut.Save();
                        }
                    }));


                    try
                    {
                        DownloadFileAsync(Fire.GetLatestWrapper().GetAwaiter().GetResult(), exePath, null).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show(Properties.Resources.ErrorDesc + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                            progress.Value = 0;
                        }));
                        return;
                    }

                    Dispatcher.Invoke(new Action(() =>
                    {
                        if (chkRun.IsChecked ?? false)
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = exePath,
                                WorkingDirectory = xPath
                            });
                    }));
                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    Dispatcher.Invoke(new Action(() => MessageBox.Show(Properties.Resources.ErrorDesc + e.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error)));
                }
            }).Start();
            btnMinimize.Click += (a, b) => WindowState = WindowState.Minimized;
            MouseDown += (a, b) =>
            {
                try
                {
                    DragMove();
                }
                catch (Exception)
                {

                }
            };
        }


        public static async Task DownloadFileAsync(string url, string fileName, Action<int> onProgress)
        {
            using (var client = new HttpClient())
            {
                byte[] buffer = new byte[2048];
                long read = 0;
                long len;

                var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                len = resp.Content.Headers.ContentLength ?? 1;
                
                using (var stream = await resp.Content.ReadAsStreamAsync())
                using (var file = System.IO.File.Create(fileName))
                {
                    int r;
                    while((r = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await file.WriteAsync(buffer, 0, r);

                        read += r;
                        onProgress?.Invoke((int)(read * 100 / len));
                    }
                }
            }
        }
    }
}
