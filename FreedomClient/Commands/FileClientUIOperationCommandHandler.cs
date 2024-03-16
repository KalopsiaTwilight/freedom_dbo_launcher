using FreedomClient.Core;
using FreedomClient.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace FreedomClient.Commands
{
    public abstract class FileClientUIOperationCommandHandler
    {
        protected readonly VerifiedFileClient _fileClient;
        protected readonly ApplicationState _appState;
        protected readonly ILogger _logger;

        private readonly Stopwatch _operationTimer;
        private long _totalBytesDownloaded;
        private long _totalBytesVerified;
        private long _totalBytesToProcess;

        public FileClientUIOperationCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger logger)
        {
            _fileClient = fileClient;
            _appState = appState;
            _operationTimer = new Stopwatch();
            _logger = logger;

            // Wire up fileclient events

            _fileClient.ManifestVerificationStarted += OnManifestVerificationStarted;
            _fileClient.ManifestDownloadStarted += OnManifestDownloadStarted;
            _fileClient.FileDownloadStarted += OnFileDownloadStart;
            _fileClient.FileDownloadProgress += OnFileDownloadProgress;
            _fileClient.FileDownloadCompleted += OnFileDownloadCompleted;
            _fileClient.FileVerifyStarted += OnFileVerifyStarted;
            _fileClient.FileVerifyProgress += OnFileVerificationProgress;
            _fileClient.FileVerifyCompleted += OnFileVerifyCompleted;
            _fileClient.ExceptionDuringDownload += OnExceptionDuringDownload;
            _fileClient.ExceptionDuringVerify += OnExceptionDuringVerification;
        }

        protected void StartNewFileClientOperation()
        {
            _totalBytesDownloaded = 0;
            _totalBytesVerified = 0;
            _totalBytesToProcess = 0;
            _operationTimer.Restart();
        }

        protected void StopClientOperation()
        {
            _operationTimer.Stop();
        }

        private void OnManifestDownloadStarted(object? sender, ManifestDownloadStartedEventArgs e)
        {
            _totalBytesToProcess = e.ToDownload.Sum(x => x.Value.FileSize) * 2;
            _totalBytesDownloaded = 0;
            _totalBytesVerified = 0;
            _operationTimer.Restart();
        }

        private void OnManifestVerificationStarted(object? sender, ManifestVerificationStartedEventArgs e)
        {
            _totalBytesToProcess = e.ToVerify.Sum(x => x.Value.FileSize);
            _totalBytesDownloaded = 0;
            _totalBytesVerified = 0;
            _operationTimer.Restart();
        }

        private void OnFileDownloadStart(object? sender, FileDownloadStartedEventArgs e)
        {
            var totalDownloaded = _totalBytesDownloaded;
            var progress = (totalDownloaded + _totalBytesVerified) / (double)_totalBytesToProcess * 100;
            var downloadedPMs = (totalDownloaded) / (double)_operationTimer.ElapsedMilliseconds;

            var timeEst = downloadedPMs > 0
                ? TimeSpan.FromMilliseconds((_totalBytesToProcess / 2 - totalDownloaded) / downloadedPMs).ToString("hh\\:mm\\:ss")
                : "Unknown";
            _appState.UIOperation.Message = $"Downloading {Path.GetFileName(e.FilePath)}...";
            _appState.UIOperation.Progress = progress;
            _appState.UIOperation.ProgressReport = $"{BytesToString(totalDownloaded, 1)} / {BytesToString(_totalBytesToProcess / 2, 1)} ({timeEst} remaining)";
        }
        private void OnFileDownloadCompleted(object? sender, FileDownloadCompletedEventArgs e)
        {
            _totalBytesDownloaded += e.Entry.FileSize;
            _appState.UIOperation.Message = $"Downloaded {Path.GetFileName(e.FilePath)}!";
        }

        private void OnFileVerifyStarted(object? sender, FileVerifyStartedEventArgs e)
        {
            _appState.UIOperation.Message = $"Verifying {Path.GetFileName(e.FilePath)}...";
        }

        private void OnFileVerifyCompleted(object? sender, FileVerifyCompletedEventArgs e)
        {
            _totalBytesVerified += e.Entry.FileSize;
            _appState.UIOperation.Message = $"Verified {Path.GetFileName(e.FilePath)}!";
        }

        private void OnFileDownloadProgress(object? sender, FileDownloadProgressEventArgs e)
        {
            if (!_appState.UIOperation.CancellationTokenSource.IsCancellationRequested)
            {
                var totalDownloaded = _totalBytesDownloaded + e.TotalBytesRead;
                var progress = (totalDownloaded + _totalBytesVerified) / (double)_totalBytesToProcess * 100;
                var downloadedPMs = (totalDownloaded) / (double)_operationTimer.ElapsedMilliseconds;

                var fileProgress = Math.Floor((double)e.TotalBytesRead / e.Entry.FileSize * 100);
                var bytesPs = (double)e.TotalBytesRead / _fileClient.DownloadTimer.ElapsedMilliseconds * 1000;
                var timeEst = downloadedPMs > 0 
                    ? TimeSpan.FromMilliseconds((_totalBytesToProcess / 2 - totalDownloaded) / downloadedPMs).ToString("hh\\:mm\\:ss")
                    : "Unknown";
                _appState.UIOperation.Message = $"Downloading {Path.GetFileName(e.FilePath)}... ({BytesToString((long)Math.Round(bytesPs))}/s)";
                _appState.UIOperation.Progress = progress;
                _appState.UIOperation.ProgressReport = $"{BytesToString(totalDownloaded, 1)} / {BytesToString(_totalBytesToProcess / 2, 1)} ({timeEst} remaining)";
            }
        }

        private void OnFileVerificationProgress(object? sender, FileVerifyProgressEventArgs e)
        {
            if (!_appState.UIOperation.CancellationTokenSource.IsCancellationRequested)
            {
                var progress = (_totalBytesDownloaded + _totalBytesVerified + e.TotalBytesRead) / (double)_totalBytesToProcess * 100;
                var fileProgress = Math.Floor((double)e.TotalBytesRead / e.Entry.FileSize * 100);
                _appState.UIOperation.Message = $"Verifying {Path.GetFileName(e.FilePath)}... ({fileProgress}%)";
                _appState.UIOperation.Progress = progress;
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
                _appState.UIOperation.Message = "Could not validate download. Please contact a dev for help.";
                _appState.UIOperation.IsCancelled = true;
                _appState.UIOperation.Progress = 0;
                _appState.UIOperation.ProgressReport = "";
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                _appState.UIOperation.IsCancelled = true;
                _appState.UIOperation.Message = "An error occured during download. Please try again.";
                _appState.UIOperation.Progress = 0;
                _appState.UIOperation.ProgressReport = "";
                CommandManager.InvalidateRequerySuggested();
            }
            OnExceptionDuringDownload(e.Exception);
        }


        private void OnExceptionDuringVerification(object? sender, ExceptionDuringVerifyEventArgs e)
        {
            if (e.Exception is OperationCanceledException)
            {
                // No need to do anything
            }
            else if (e.Exception is AntiTamperingException)
            {
                _appState.UIOperation.Message = "Could not validate download. Please contact a dev for help.";
                _appState.UIOperation.IsCancelled = true;
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                _appState.UIOperation.IsCancelled = true;
                _appState.UIOperation.Message = "An error occured during verification of a file. Please try again.";
                CommandManager.InvalidateRequerySuggested();
            }
            OnExceptionDuringVerification(e.Exception);
        }

        protected virtual void OnExceptionDuringDownload(Exception e) { }
        protected virtual void OnExceptionDuringVerification(Exception e) { }

        protected long CalculateTotalBytesToDownload(DownloadManifest manifest)
        {
            long result = 0;
            var groups = manifest.GroupBy(x => x.Value.Source.Id);
            foreach (var group in groups)
            {
                if (group.First().Value.Source is GoogleDriveArchiveDownloadSource archive)
                {
                    result += archive.ArchiveSize;
                }
                else
                {
                    result += group.Sum(x => x.Value.FileSize);
                }
            }
            return result;
        }

        protected string BytesToString(long byteCount, int precision = 0)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), precision);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
