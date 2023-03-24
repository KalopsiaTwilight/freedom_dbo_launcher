using FreedomClient.Core;
using FreedomClient.Infrastructure;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Ookii.Dialogs.Wpf;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static System.Windows.Forms.AxHost;
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
        private readonly ILogger<MainWindow> _logger;
        private readonly Stopwatch _overallTimer;
        private readonly IHttpClientFactory _httpClientFactory;

        private CancellationTokenSource _downloadTokenSource;
        private long _totalBytesDownloaded;
        private long _totalBytesVerified;
        private long _totalBytesToProcess;

        private Timer _serverStatusTimer;

        public MainWindow(VerifiedFileClient fileClient, ApplicationState state, ILogger<MainWindow> logger, IHttpClientFactory httpClientFactory)
        {
            _downloadTokenSource = new CancellationTokenSource();
            _overallTimer = new Stopwatch();

            _httpClientFactory = httpClientFactory;
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

            bgImage.ImagePaths = _appState.LauncherImages;
            UpdateLauncherImages();

            _serverStatusTimer = new Timer(new TimerCallback(UpdateServerStatus), null, 0, 10000);
            TestLatestVersion();
        }

        private async void CheckForUpdates()
        {
            _logger.LogInformation("Checking for client files updates...");
            DownloadManifest latestManifest;
            try
            {
                latestManifest = await _fileClient.GetManifest(_downloadTokenSource.Token);
            }
            catch (HttpRequestException exc)
            {
                await Dispatcher.BeginInvoke(() =>
                {
                    txtProgress.Text = "Unable to connect to Freedom's CDN to check for updates." + Environment.NewLine +
                    "You might not be able to log in.";
                    btnMain.IsEnabled = true;
                });
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                _logger.LogError(exc, null);
                return;
            }

            if (!latestManifest.Equals(_appState.LastManifest))
            {
                _logger.LogInformation("Starting client files update...");
                // Get update manifest here
                var patchManifest = latestManifest.CreatePatchManifestFrom(_appState.LastManifest);
                _appState.LoadState = ApplicationLoadState.CheckForUpdate;
                _downloadTokenSource.Dispose();
                _downloadTokenSource = new CancellationTokenSource();
                _totalBytesToProcess = CalculateTotalBytesToDownload(patchManifest) + patchManifest.Sum(x => x.Value.FileSize); 
                _totalBytesDownloaded = 0;
                _totalBytesVerified = 0;
                await Dispatcher.BeginInvoke(() =>
                {
                    txtProgress.Text = "Updating...";
                });
                await _fileClient.VerifyFiles(patchManifest, _appState.InstallPath, _downloadTokenSource.Token);
                _overallTimer.Stop();


                _logger.LogInformation("Client files update succesful! Clearing cache...");
                await Dispatcher.BeginInvoke(() =>
                {
                    txtProgress.Text = "Clearing cache...";
                });
                var cachePath = Path.Combine(_appState.InstallPath!, "_retail_/Cache");
                if (Directory.Exists(cachePath))
                {
                    foreach (var file in Directory.EnumerateFiles(cachePath, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.Delete(file);
                        } catch
                        {
                            // Just eat the exception here, no reason to let an update fail on this and practically speaking, this should never be hit.
                        }
                    }
                    RemoveEmptyDirectories(cachePath);
                }

                _appState.LastManifest = latestManifest;
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                await Dispatcher.BeginInvoke(() =>
                {
                    btnCancelDownload.Visibility = Visibility.Hidden;
                    pgbProgress.Value = 100;
                    txtProgress.Text = "Ready to launch!";
                    txtOverallProgress.Text = "";
                    btnMain.IsEnabled = true;
                });
                return;
            }
            if (!CheckRequiredFilesExist(_appState.LastManifest))
            {
                _logger.LogInformation("Found missing files, starting restore..");
                await Dispatcher.BeginInvoke(() =>
                {
                    txtProgress.Text = "Restoring missing files...";
                    btnMain.Content = "Install";
                });
                VerifyInstall();
                return;
            }
            _logger.LogInformation("Client files are up to date!");
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
            foreach (var entry in manifest)
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
            _logger.LogInformation("Starting installation restore...");
            _appState.LoadState = ApplicationLoadState.VerifyingFiles;
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
            _logger.LogInformation("Installation restored!");

            if (completeReset)
            {
                _logger.LogInformation("Removing files not included with install...");
                await Dispatcher.BeginInvoke(() => { txtProgress.Text = "Removing files not included with install..."; });
                foreach (var file in Directory.EnumerateFiles(_appState.InstallPath!, "*", SearchOption.AllDirectories))
                {
                    var key = file.Substring(_appState.InstallPath!.Length + 1).Replace("\\", "/");
                    if (!manifest.ContainsKey(key))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, null);
                            _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                            await Dispatcher.BeginInvoke(() =>
                            {
                                pgbProgress.Value = 100;
                                btnCancelDownload.Visibility = Visibility.Hidden;
                                txtProgress.Text = "Failed to delete extra files, install files were restored.";
                                txtOverallProgress.Text = "";
                                btnMain.IsEnabled = true;
                                btnMain.Content = "Launch";
                            });
                            return;
                        }
                    }
                }
                RemoveEmptyDirectories(_appState.InstallPath);
                _logger.LogInformation("Files not included with install removed!");
            }
            _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
            await Dispatcher.BeginInvoke(() =>
            {
                pgbProgress.Value = 100;
                btnCancelDownload.Visibility = Visibility.Hidden;
                txtProgress.Text = "Ready to launch!";
                txtOverallProgress.Text = "";
                btnMain.IsEnabled = true;
                btnMain.Content = "Launch";
            });
        }

        #region UI Events

        private async void btnMain_Click(object sender, RoutedEventArgs e)
        {
            btnMain.IsEnabled = false;

            if (_appState.LoadState != ApplicationLoadState.ReadyToLaunch)
            {
                var folderDialog = new VistaFolderBrowserDialog();
                if (folderDialog.ShowDialog() != true)
                {
                    btnMain.IsEnabled = true;
                    return;
                }


                _downloadTokenSource.Dispose();
                _downloadTokenSource = new CancellationTokenSource();
                btnCancelDownload.Visibility = Visibility.Visible;
                txtProgress.Text = "Downloading manifest...";
                DownloadManifest manifest;
                try
                {
                    manifest = await _fileClient.GetManifest(_downloadTokenSource.Token);
                }
                catch (HttpRequestException exc)
                {
                    _logger.LogError(exc, null);
                    txtProgress.Text = "Unable to connect to Freedom's CDN. Please try again later.";
                    return;
                }
                _appState.LastManifest = manifest;

                var driveInfo = new DriveInfo(folderDialog.SelectedPath);
                var totalDownloadSize = manifest.Sum(x => x.Value.FileSize);
                if (driveInfo.AvailableFreeSpace < totalDownloadSize)
                {
                    txtProgress.Text = $"Not enough free space on drive. {BytesToString(totalDownloadSize)} is required.";
                    return;
                }

                _appState.InstallPath = folderDialog.SelectedPath;
                if (!Directory.Exists(_appState.InstallPath))
                {
                    Directory.CreateDirectory(_appState.InstallPath);
                }

                pgbProgress.Visibility = Visibility.Visible;
                _totalBytesToProcess = totalDownloadSize * 2;
                _totalBytesDownloaded = 0;
                _totalBytesVerified = 0;
                await _fileClient.VerifyFiles(manifest, _appState.InstallPath, _downloadTokenSource.Token);
                _overallTimer.Stop();
                _downloadTokenSource.Cancel();

                pgbProgress.Value = 100;
                txtProgress.Text = "Successfully installed! Client is now ready to launch";
                txtOverallProgress.Text = "";
                btnCancelDownload.Visibility = Visibility.Hidden;
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
            RemoveEmptyDirectories(_appState.InstallPath);
            Dispatcher.Invoke(() =>
            {
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
            _totalBytesToProcess = CalculateTotalBytesToDownload(e.ToDownload) + e.ToDownload.Sum(x => x.Value.FileSize);
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
                    var totalDownloaded = _totalBytesDownloaded + e.TotalBytesRead;
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
                btnMain.IsEnabled = true;
                txtProgress.Text = "An error occured during download. Please try again.";
            }
        }

        private void OnExceptionDuringVerification(object? sender, ExceptionDuringVerifyEventArgs e)
        {
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
                btnMain.IsEnabled = true;
                txtProgress.Text = "An error occured during verification of a file. Please try again.";
            }
        }
        #endregion

        #region Utility

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

        void RemoveEmptyDirectories(string path)
        {
            foreach (var subDirectory in Directory.GetDirectories(path))
            {
                RemoveEmptyDirectories(subDirectory);
                if (Directory.EnumerateFiles(subDirectory, "*", SearchOption.AllDirectories).Count() == 0)
                {
                    Directory.Delete(subDirectory);
                }
            }
        }

        #endregion

        private async void UpdateLauncherImages()
        {
            _logger.LogInformation("Updating launcher images...");
            var credential = GoogleCredential
                .FromStream(new EmbeddedFileProvider(Assembly.GetEntryAssembly()).GetFileInfo(Constants.GoogleCredentialsJsonPath).CreateReadStream())
                .CreateScoped(DriveService.Scope.Drive);
            var service = new DriveService(new BaseClientService.Initializer()
            {
                ApplicationName = Constants.AppIdentifier,
                HttpClientInitializer = credential
            });
            var listFilesRequest = service.Files.List();
            listFilesRequest.SupportsAllDrives = true;
            listFilesRequest.IncludeItemsFromAllDrives = true;
            listFilesRequest.Q = $"'{Constants.LauncherImagesDriveFolderId}' in parents";
            var listFilesResponse = await listFilesRequest.ExecuteAsync();
            if (listFilesResponse == null)
            {
                _logger.LogError("Unable to download images from google drive");
                return;
            }
            var imageCollection = new List<string>();
            foreach (var file in listFilesResponse.Files)
            {
                var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.AppIdentifier, "launcherImages", file.Name);
                if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                }
                if (!File.Exists(outputPath))
                {
                    var downloadFileRequest = service.Files.Get(file.Id);
                    downloadFileRequest.AcknowledgeAbuse = true;
                    using (var fileStream = File.Create(outputPath))
                    {
                        var progress = await downloadFileRequest.DownloadAsync(fileStream);

                        if (progress.BytesDownloaded == 0)
                        {
                            throw new InvalidDataException();
                        }
                    }
                }
                imageCollection.Add(outputPath);
            }
            imageCollection = imageCollection.OrderBy(x => Path.GetFileName(x)).ToList();
            Dispatcher.Invoke(() =>
            {
                bgImage.ImagePaths = imageCollection;
            });
            // Clean up old images
            foreach (var img in _appState.LauncherImages)
            {
                if (!imageCollection.Contains(img))
                {
                    File.Delete(img);
                }
            }
            _appState.LauncherImages = imageCollection;
            _logger.LogInformation("Launcher images updated!");
        }

        private async void UpdateServerStatus(object? state)
        {
            var client = _httpClientFactory.CreateClient();
            var resp = await client.PostAsync(Constants.MinimanagerUrl + "/Data/StatusLinePartial", null);
            Color toSet = Color.FromRgb(51, 51, 51);
            if (resp.IsSuccessStatusCode)
            {
                var statusText = await resp.Content.ReadAsStringAsync();
                var match = Regex.Match(statusText, ".*status-(\\w+)");
                if (match.Success)
                {
                    switch (match.Groups[1].Value)
                    {
                        case "good": toSet = Color.FromRgb(76, 185, 68); break;
                        case "loading": toSet = Color.FromRgb(245, 166, 91); break;
                        case "bad": toSet = Color.FromRgb(137, 2, 62); break;
                    }
                }

            }
            await srvStatusIndicator.Dispatcher.BeginInvoke(() =>
            {
                srvStatusIndicator.Color = toSet;
            });
        }

        private async void TestLatestVersion()
        {
            _logger.LogInformation("Checking for launcher updates...");
            var client = _httpClientFactory.CreateClient();
            try
            {
                var resp = await client.GetAsync(Constants.CdnUrl + "/latestClientVersion.txt");
                if (resp.IsSuccessStatusCode)
                {
                    var versionTxt = await resp.Content.ReadAsStringAsync();
                    if (Version.Parse(_appState.Version) < Version.Parse(versionTxt))
                    {
                        _logger.LogInformation($"A launcher update is available, new version: {versionTxt}.");
                        var result = MessageBox.Show("A new launcher version is available, would you like to download it now?", "New version available", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            var pStart = new ProcessStartInfo()
                            {
                                UseShellExecute = true,
                                FileName = "https://github.com/KalopsiaTwilight/freedom_client/releases/"
                            };
                            Process.Start(pStart);
                            Close();
                        }
                    } else
                    {
                        _logger.LogInformation($"Launcher is up to date!");
                    }
                }
            }
            catch(HttpRequestException exc)
            {
                _logger.LogError(exc, null);
            }
        }

        private long CalculateTotalBytesToDownload(DownloadManifest manifest)
        {
            long result = 0;
            var groups = manifest.GroupBy(x => x.Value.Source.Id);
            foreach(var group in groups)
            {
                if (group.First().Value.Source is GoogleDriveArchiveDownloadSource archive)
                {
                    result += archive.ArchiveSize;
                } else
                {
                    result += group.Sum(x => x.Value.FileSize);
                }
            }
            return result;
        }
    }
}
