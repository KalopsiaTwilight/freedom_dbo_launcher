using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FreedomClient.Commands
{
    public class DownloadWoWClientFilesCommand : IRequest
    {
    }

    public class DownloadWoWClientFilesCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<DownloadWoWClientFilesCommand>
    {
        public DownloadWoWClientFilesCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger<LaunchWoWCommandHandler> logger)
            : base(fileClient, appState, logger)
        {
        }

        public async Task Handle(DownloadWoWClientFilesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Downloading WoW Client Files",
                    IsCancellable = true,
                    Message = "Installing WoW...",
                    Progress = 0
                };
                var folderDialog = new VistaFolderBrowserDialog();
                if (folderDialog.ShowDialog() != true)
                {
                    return;
                }

                _appState.UIOperation.Message = "Downloading manifest...";
                DownloadManifest manifest;
                try
                {
                    manifest = await _fileClient.GetManifest(_appState.UIOperation.CancellationTokenSource.Token);
                }
                catch (HttpRequestException exc)
                {
                    _logger.LogError(exc, null);
                    _appState.UIOperation.Message = "Unable to connect to Freedom's CDN. Please try again later.";
                    return;
                }
                _appState.LastManifest = manifest;

                var driveInfo = new DriveInfo(folderDialog.SelectedPath);
                var totalDownloadSize = manifest.Sum(x => x.Value.FileSize);
                if (driveInfo.AvailableFreeSpace < totalDownloadSize)
                {
                    _appState.UIOperation.Message = $"Not enough free space on drive. {BytesToString(totalDownloadSize)} is required.";
                    return;
                }

                _appState.InstallPath = folderDialog.SelectedPath;
                if (!Directory.Exists(_appState.InstallPath))
                {
                    Directory.CreateDirectory(_appState.InstallPath);
                }

                await _fileClient.VerifyFiles(manifest, _appState.InstallPath, _appState.UIOperation.CancellationTokenSource.Token);

                _appState.UIOperation.Progress = 100;
                _appState.UIOperation.Message = "Successfully installed! Client is now ready to launch";
                _appState.UIOperation.ProgressReport = "";
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
            }
            catch(OperationCanceledException)
            {
                _appState.UIOperation.Message = "Installation cancelled.";
                _appState.UIOperation.IsCancelled = true;
            }
        }
    }
}
