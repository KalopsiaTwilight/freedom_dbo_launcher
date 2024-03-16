using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.ViewModels.Dbo;
using MediatR;
using Microsoft.Extensions.Logging;
using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FreedomClient.Commands
{
    public class DownloadDboClientFilesCommand : IRequest
    {
        public DboShellViewModel ShellViewModel { get; set; }
        public DownloadDboClientFilesCommand(DboShellViewModel shellViewModel)
        {
            ShellViewModel = shellViewModel;
        }
    }

    public class DownloadDboClientFilesCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<DownloadDboClientFilesCommand>
    {
        public DownloadDboClientFilesCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger<DownloadDboClientFilesCommandHandler> logger)
            : base(fileClient, appState, logger)
        {
        }

        public async Task Handle(DownloadDboClientFilesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Downloading DBOG Client Files",
                    IsCancellable = true,
                    Message = "Installing DBOG...",
                    Progress = 0
                };
                var folderDialog = new VistaFolderBrowserDialog();
                if (folderDialog.ShowDialog() != true)
                {
                    return;
                }

                _appState.UIOperation.Message = "Downloading manifest...";
                _appState.LoadState = ApplicationLoadState.CheckForUpdate;
                DownloadManifest manifest;
                try
                {
                    manifest = await _fileClient.GetManifest(Constants.DboManifestUrl, Constants.DboSignatureUrl, _appState.UIOperation.CancellationTokenSource.Token);
                }
                catch (HttpRequestException exc)
                {
                    _logger.LogError(exc, null);
                    _appState.LoadState = ApplicationLoadState.NotInstalled;
                    _appState.UIOperation.Message = "Unable to connect to Freedom's CDN. Please try again later.";
                    _appState.UIOperation.IsFinished = true;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }
                _appState.LastManifest = manifest;

                var driveInfo = new DriveInfo(folderDialog.SelectedPath);
                var totalDownloadSize = manifest.Sum(x => x.Value.FileSize);
                if (driveInfo.AvailableFreeSpace < totalDownloadSize)
                {
                    _appState.UIOperation.Message = $"Not enough free space on drive. {BytesToString(totalDownloadSize)} is required.";
                    _appState.UIOperation.IsFinished = true;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                if (!Directory.Exists(folderDialog.SelectedPath))
                {
                    Directory.CreateDirectory(folderDialog.SelectedPath);
                }

                await _fileClient.EnsureFilesInManifest(manifest, folderDialog.SelectedPath, _appState.UIOperation.CancellationTokenSource.Token);

                _appState.InstallPath = folderDialog.SelectedPath;
                _appState.UIOperation.Progress = 100;
                _appState.UIOperation.Message = "Successfully installed! Client is now ready to launch";
                _appState.UIOperation.ProgressReport = "";
                _appState.UIOperation.IsFinished = true;
                request.ShellViewModel.IsInstalled = true;
                CommandManager.InvalidateRequerySuggested();
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                CommandManager.InvalidateRequerySuggested();
            }
            catch(OperationCanceledException)
            {
                _appState.LoadState = ApplicationLoadState.NotInstalled;
                _appState.UIOperation.Message = "Installation cancelled.";
                _appState.UIOperation.IsCancelled = true;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        protected override void OnExceptionDuringDownload(Exception e)
        {
            _appState.LoadState = ApplicationLoadState.NotInstalled;
            CommandManager.InvalidateRequerySuggested();
        }

        protected override void OnExceptionDuringVerification(Exception e)
        {
            _appState.LoadState = ApplicationLoadState.NotInstalled;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
