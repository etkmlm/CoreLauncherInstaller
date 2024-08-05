using IWshRuntimeLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
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
                    var newest = Fire.GetLatestFile();
                    string xPath = Path.Combine(path, "Core Launcher");
                    string jarPath = Path.Combine(xPath, "CoreLauncher.jar");
                    string exePath = Path.Combine(xPath, "CoreLauncher.exe");

                    if (System.IO.File.Exists(jarPath))
                    {
                        Dispatcher.Invoke(new Action(() => MessageBox.Show(Properties.Resources.AlreadyInstalled, Properties.Resources.Oops, MessageBoxButton.OK, MessageBoxImage.Error)));
                        return;
                    }
                    using (var client = new WebClient())
                    {
                        client.DownloadProgressChanged += (c, d) => Dispatcher.Invoke(new Action(() => progress.Value = d.ProgressPercentage));
                        client.DownloadFileCompleted += (c, d) =>
                        {
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
                                client.DownloadFile(Fire.GetLatestWrapper(), exePath);
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
                        };
                        Directory.CreateDirectory(xPath);
                        client.DownloadFileAsync(new Uri(newest.URL), jarPath);
                    }
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
    }
}
