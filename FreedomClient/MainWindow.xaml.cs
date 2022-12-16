using FreedomClient.Core;
using FreedomClient.Infrastructure;
using Microsoft.Extensions.Logging;
using Ookii.Dialogs.Wpf;
using Org.BouncyCastle.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FreedomClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly VerifiedFileClient _fileClient;
        private readonly ApplicationState _appState;
        private readonly ILogger _logger;
        private readonly Stopwatch _overallTimer;

        private CancellationTokenSource _downloadTokenSource;
        private long _totalBytesDownloaded;
        private long _totalBytesVerified;
        private long _totalBytesToProcess;

        public MainWindow(VerifiedFileClient fileClient, ApplicationState state, ILogger logger)
        {
            _downloadTokenSource = new CancellationTokenSource();
            _overallTimer = new Stopwatch();

            _fileClient = fileClient;
            _appState = state;
            _logger = logger;
            _totalBytesDownloaded = 0;
            _totalBytesVerified = 0;
            _totalBytesToProcess = 0;

            // Wire up event handlers for file client
            _fileClient.ManifestDownloadStarted += OnManifestDownloadStarted;
            _fileClient.FileDownloadStarted += OnFileDownloadStart;
            _fileClient.FileDownloadProgress += OnFileDownloadProgress;
            _fileClient.FileDownloadCompleted += OnFileDownloadCompleted;
            _fileClient.FileVerifyStarted += OnFileVerifyStarted;
            _fileClient.FileVerifyProgress += OnFileVerificationProgress;
            _fileClient.FileVerifyCompleted += OnFileVerifyCompleted;
            _fileClient.ExceptionDuringDownload += OnExceptionDuringDownload;
            _fileClient.ExceptionDuringVerify += OnExceptionDuringVerification;


            InitializeComponent();
            if (state.LoadState == ApplicationLoadState.NotInstalled)
            {
                btnMain.Content = "Install";
            }
            else
            {
                btnMain.Content = "Launch";
                btnMain.IsEnabled = false;
                txtProgress.Text = "Checking for updates...";
                CheckForUpdates();
            }
        }

        private async void CheckForUpdates()
        {
            var updateReady = await _fileClient.CheckForUpdates(_appState.LastManifest, _downloadTokenSource.Token);
            if (updateReady)
            {
                _downloadTokenSource = new CancellationTokenSource();
                await Dispatcher.BeginInvoke(() => { txtProgress.Text = "Updating..."; });
                var manifest = await _fileClient.GetManifest(_downloadTokenSource.Token);
                _appState.LastManifest = manifest;
                VerifyInstall();
                return;
            } 
            if(!CheckRequiredFilesExist(_appState.LastManifest))
            {
                VerifyInstall();
                return;
            }
            _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
            await Dispatcher.BeginInvoke(() =>
            {
                pgbProgress.Value = 100;
                txtProgress.Text = "Ready to launch!";
                btnMain.IsEnabled = true;
            });
        }

        private bool CheckRequiredFilesExist(DownloadManifest manifest)
        {
            foreach(var entry in manifest)
            {
                var filePath = Path.Combine(_appState.InstallPath, entry.Key);
                if (!File.Exists(filePath))
                {
                    return false;
                }
            }
            return true;
        }

        public async void VerifyInstall(bool completeReset = false)
        {
            await Dispatcher.BeginInvoke(() =>
            {
                btnMain.IsEnabled = false;
                txtProgress.Text = "Verifying mandatory files integrity...";
                btnCancelDownload.Visibility = Visibility.Visible;
            });
            var manifest = _appState.LastManifest;

            _downloadTokenSource.Dispose();
            _downloadTokenSource = new CancellationTokenSource();
            _totalBytesToProcess = manifest.Sum(x => x.Value.FileSize);
            _totalBytesDownloaded = 0;
            _totalBytesVerified = 0;
            await _fileClient.VerifyFiles(manifest, _appState.InstallPath!, _downloadTokenSource.Token);
            _overallTimer.Stop();
            _downloadTokenSource.Cancel();

            if (completeReset)
            {
                await Dispatcher.BeginInvoke(() => { txtProgress.Text = "Removing files not included with install..."; });
                foreach (var file in Directory.EnumerateFiles(_appState.InstallPath!, "*", SearchOption.AllDirectories))
                {
                    var key = file.Substring(_appState.InstallPath!.Length + 1).Replace("\\", "/");
                    if (!manifest.ContainsKey(key))
                    {
                        File.Delete(file);
                    }
                }
                RemoveEmptyDirectores(_appState.InstallPath);
            }
            await Dispatcher.BeginInvoke(() =>
            {
                pgbProgress.Value = 100;
                txtProgress.Text = "Ready to launch!";
                txtOverallProgress.Text = "";
                btnMain.IsEnabled = true;
            });
        }

        #region UI Events

        private async void btnMain_Click(object sender, RoutedEventArgs e)
        {
            btnMain.IsEnabled = false;

            if (_appState.LoadState == ApplicationLoadState.NotInstalled)
            {
                var folderDialog = new VistaFolderBrowserDialog();
                if (folderDialog.ShowDialog() != true)
                {
                    btnMain.IsEnabled = true;
                    return;
                }

                _appState.InstallPath = folderDialog.SelectedPath;
                if (!Directory.Exists(_appState.InstallPath))
                {
                    Directory.CreateDirectory(_appState.InstallPath);
                }

                _downloadTokenSource.Dispose();
                _downloadTokenSource = new CancellationTokenSource();
                btnCancelDownload.Visibility = Visibility.Visible;
                txtProgress.Text = "Downloading manifest...";
                var manifest = await _fileClient.GetManifest(_downloadTokenSource.Token);
                _appState.LastManifest = manifest;
                pgbProgress.Visibility = Visibility.Visible;

                _totalBytesToProcess = manifest.Sum(x => x.Value.FileSize) * 2;
                _totalBytesDownloaded = 0;
                _totalBytesVerified = 0;
                await _fileClient.VerifyFiles(manifest, _appState.InstallPath, _downloadTokenSource.Token);
                _overallTimer.Stop();
                _downloadTokenSource.Cancel();

                pgbProgress.Value = 100;
                txtProgress.Text = "Successfully installed! Client is now ready to launch";
                txtOverallProgress.Text = "";
                btnMain.Content = "Launch";
                btnMain.IsEnabled = true;
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
            }
            else
            {
                var pInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = Path.Combine(_appState.InstallPath, "Arctium WoW Launcher.exe"),
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                txtProgress.Text = "Launching WoW...";
                pgbProgress.Value = 0;
                var process = Process.Start(pInfo);
                MeasureLaunchProgress(process);
            }
        }

        private async void MeasureLaunchProgress(Process process)
        {
            var linesRead = 0;
            var expectedLines = 297;
            while (!process.HasExited)
            {
                await process.StandardOutput.ReadLineAsync();
                linesRead++;
                Dispatcher.Invoke(() =>
                {
                    pgbProgress.Value = (double)linesRead / expectedLines * 100;
                });
            }
            if (process.ExitCode == 0)
            {
                Close();
            }
            else
            {
                var stdErr = process.StandardError.ReadToEnd();
                _logger.LogError("Encountered error during launching Arctium Launcher. Following is stderr:");
                _logger.LogError(stdErr);
                Dispatcher.Invoke(() =>
                {
                    btnMain.IsEnabled = true;
                    txtProgress.Text = "Something went wrong during the launch. Please contact a dev.";
                });
            }
        }

        private void btnForums_Click(object sender, RoutedEventArgs e)
        {
            var pStart = new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = "https://wowfreedom-rp.com/f/"
            };
            Process.Start(pStart);
        }
        private void btnStatus_Click(object sender, RoutedEventArgs e)
        {
            var pStart = new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = "https://mm.wowfreedom-rp.com/Home/Status"
            };
            Process.Start(pStart);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow(this, _appState);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void btnCancelDownload_Click(object sender, RoutedEventArgs e)
        {
            _downloadTokenSource.Cancel();
            btnCancelDownload.Visibility = Visibility.Hidden;
            txtProgress.Text = "Operation cancelled";
            txtOverallProgress.Text = "";
            pgbProgress.Value = 0;
            btnMain.IsEnabled = _appState.LoadState == ApplicationLoadState.NotInstalled ? false : true;
        }

        private async void CleanupInstallFiles()
        {
            // Wait for cancellation to propogate
            await Task.Delay(1000);
            // Cleanup files
            foreach (var file in Directory.EnumerateFiles(_appState.InstallPath!, "*", SearchOption.AllDirectories))
            {
                var key = file.Substring(_appState.InstallPath!.Length + 1).Replace("\\", "/");
                if (_appState.LastManifest.ContainsKey(key))
                {
                    File.Delete(file);
                }
            }
            RemoveEmptyDirectores(_appState.InstallPath);
            Dispatcher.Invoke(() => {
                txtProgress.Text = "Installation cancelled.";
            });
        }

        private void MenuGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        #endregion

        #region FileClient_Events

        private void OnManifestDownloadStarted(object? sender, ManifestDownloadStartedEventArgs e)
        {
            _totalBytesToProcess = e.ToDownload.Sum(x => x.Value.FileSize) * 2;
            _totalBytesDownloaded = 0;
            _totalBytesVerified = 0;
            _overallTimer.Restart();
        }

        private void OnFileDownloadStart(object? sender, FileDownloadStartedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Downloading {Path.GetFileName(e.FilePath)}...";
            });
        }
        private void OnFileDownloadCompleted(object? sender, FileDownloadCompletedEventArgs e)
        {
            _totalBytesDownloaded += e.Entry.FileSize;
            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Downloaded {Path.GetFileName(e.FilePath)}!";
            });
        }

        private void OnFileVerifyStarted(object? sender, FileVerifyStartedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Verifying {Path.GetFileName(e.FilePath)}...";
            });
        }

        private void OnFileVerifyCompleted(object? sender, FileVerifyCompletedEventArgs e)
        {
            _totalBytesVerified += e.Entry.FileSize;
            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Verified {Path.GetFileName(e.FilePath)}!";
            });
        }

        private void OnFileDownloadProgress(object? sender, FileDownloadProgressEventArgs e)
        {
            if (!_downloadTokenSource.IsCancellationRequested)
            {
                Dispatcher.Invoke(() =>
                {
                    var totalDownloaded =  _totalBytesDownloaded + e.TotalBytesRead;
                    var progress = (totalDownloaded + _totalBytesVerified) / (double)_totalBytesToProcess * 100;
                    var downloadedPMs = (totalDownloaded) / (double)_overallTimer.ElapsedMilliseconds;
                    var fileProgress = Math.Floor((double)e.TotalBytesRead / e.Entry.FileSize * 100);
                    var bytesPs = (double)e.TotalBytesRead / _fileClient.DownloadTimer.ElapsedMilliseconds * 1000;
                    var timeEst = TimeSpan.FromMilliseconds((_totalBytesToProcess / 2 - totalDownloaded) / downloadedPMs);
                    txtProgress.Text = $"Downloading {Path.GetFileName(e.FilePath)}... ({BytesToString((long)Math.Round(bytesPs))}/s)";
                    pgbProgress.Value = progress;
                    txtOverallProgress.Text = $"{BytesToString(totalDownloaded, 1)} / {BytesToString(_totalBytesToProcess / 2, 1)} ({timeEst:hh\\:mm\\:ss} remaining)";
                });
            }
        }

        private void OnFileVerificationProgress(object? sender, FileVerifyProgressEventArgs e)
        {
            if (!_downloadTokenSource.IsCancellationRequested)
            {
                Dispatcher.Invoke(() =>
                {
                    var progress = (_totalBytesDownloaded + _totalBytesVerified + e.TotalBytesRead) / (double)_totalBytesToProcess * 100;
                    var fileProgress = Math.Floor((double)e.TotalBytesRead / e.Entry.FileSize * 100);
                    txtProgress.Text = $"Verifying {Path.GetFileName(e.FilePath)}... ({fileProgress}%)";
                    pgbProgress.Value = progress;
                });
            }
        }

        private void OnExceptionDuringDownload(object? sender, ExceptionDuringDownloadEventArgs e)
        {
            btnMain.IsEnabled = true;
            if (e.Exception is OperationCanceledException)
            {
                // No need to do anything
            }
            else if (e.Exception is AntiTamperingException)
            {
                txtProgress.Text = "Could not validate download. Please contact a dev for help.";
            }
            else
            {
                txtProgress.Text = "An error occured during download. Please try again.";
            }
        }

        private void OnExceptionDuringVerification(object? sender, ExceptionDuringVerifyEventArgs e)
        {
            btnMain.IsEnabled = true;
            if (e.Exception is OperationCanceledException)
            {
                // No need to do anything
            }
            else
            {
                txtProgress.Text = "An error occured during verification of a file. Please try again.";
            }
        }
        #endregion

        static string BytesToString(long byteCount, int precision = 0)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), precision);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        void RemoveEmptyDirectores(string path)
        {
            foreach (var subDirectory in Directory.GetDirectories(path))
            {
                RemoveEmptyDirectores(subDirectory);
                if (Directory.EnumerateFiles(subDirectory, "*", SearchOption.AllDirectories).Count() == 0)
                {
                    Directory.Delete(subDirectory);
                }
            }
        }
    }
}
