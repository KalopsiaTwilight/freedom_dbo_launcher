﻿using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.ViewModels.WoW;
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
    public class DownloadWoWClientFilesCommand : IRequest
    {
        public WoWShellViewModel ShellViewModel { get; set; }
        public DownloadWoWClientFilesCommand(WoWShellViewModel shellViewModel)
        {
            ShellViewModel = shellViewModel;
        }
    }

    public class DownloadWoWClientFilesCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<DownloadWoWClientFilesCommand>
    {
        public DownloadWoWClientFilesCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger<DownloadWoWClientFilesCommandHandler> logger)
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
                _appState.LoadState = ApplicationLoadState.CheckForUpdate;
                DownloadManifest manifest;
                try
                {
                    manifest = await _fileClient.GetManifest(_appState.UIOperation.CancellationTokenSource.Token);
                }
                catch (HttpRequestException exc)
                {
                    _logger.LogError(exc, null);
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

                _appState.InstallPath = folderDialog.SelectedPath;
                if (!Directory.Exists(_appState.InstallPath))
                {
                    Directory.CreateDirectory(_appState.InstallPath);
                }

                await _fileClient.EnsureFilesInManifest(manifest, _appState.InstallPath, _appState.UIOperation.CancellationTokenSource.Token);

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
                _appState.UIOperation.Message = "Installation cancelled.";
                _appState.UIOperation.IsCancelled = true;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
