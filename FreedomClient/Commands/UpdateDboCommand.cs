﻿using FreedomClient.Core;
using FreedomClient.Models;
using Google.Apis.Logging;
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
using System.Windows.Input;
using System.Windows.Threading;

namespace FreedomClient.Commands
{
    public class UpdateDboCommand: IRequest
    {
    }

    public class UpdateDboCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<UpdateDboCommand>
    {
        private readonly IMediator _mediator;

        public UpdateDboCommandHandler(ApplicationState applicationState, ILogger<UpdateDboCommandHandler> logger, VerifiedFileClient fileClient, IMediator mediator)
            : base(fileClient, applicationState, logger)
        {
            _mediator = mediator;
        }

        public async Task Handle(UpdateDboCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_appState.InstallPath)) {
                return;
            }
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    IsCancellable = true,
                    Name = "Updating Dbo",
                    Message = "Checking for updates...",
                    Progress = 0
                };
                var uiCancelToken = _appState.UIOperation.CancellationTokenSource.Token;
                if (!CheckInstallPath())
                {
                    return;
                }

                _logger.LogInformation("Checking for client files updates...");
                // Checkout latest manifest
                DownloadManifest latestManifest;
                try
                {
                    latestManifest = await _fileClient.GetManifest(Constants.DboManifestUrl, Constants.DboSignatureUrl, _appState.UIOperation.CancellationTokenSource.Token);
                }
                catch (HttpRequestException exc)
                {
                    _appState.UIOperation.Message = "Unable to connect to Freedom's CDN to check for updates." + Environment.NewLine +
                        "You might not be able to log in.";
                    _appState.UIOperation.Progress = 100;
                    _appState.UIOperation.IsFinished = true;
                    CommandManager.InvalidateRequerySuggested();
                    _logger.LogError(exc, null);
                    return;
                }
                uiCancelToken.ThrowIfCancellationRequested();
                if (latestManifest.Equals(_appState.LastManifest))
                {
                    // No update needed
                    _logger.LogInformation("Client files are up to date!");
                    _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                    _appState.UIOperation.Progress = 100;
                    _appState.UIOperation.Message = "Ready to launch!";
                    _appState.UIOperation.IsFinished = true;
                    CommandManager.InvalidateRequerySuggested();
                }
                else
                {
                    // Update needed
                    _logger.LogInformation("Starting client files update...");
                    // Get update manifest
                    var patchManifest = latestManifest.CreatePatchManifestFrom(_appState.LastManifest);
                    _appState.LoadState = ApplicationLoadState.CheckForUpdate;
                    _appState.UIOperation.Message = "Updating...";
                    await _fileClient.EnsureFilesInManifest(patchManifest, _appState.InstallPath, uiCancelToken);
                    _appState.UIOperation.ProgressReport = string.Empty;

                    uiCancelToken.ThrowIfCancellationRequested();

                    _logger.LogInformation("Client files update succesful! Clearing cache...");
                    _appState.UIOperation.Message = "Clearing cache...";
                    var cachePath = Path.Combine(_appState.InstallPath, "_retail_/Cache");
                    if (Directory.Exists(cachePath))
                    {
                        foreach (var file in Directory.EnumerateFiles(cachePath, "*", SearchOption.AllDirectories))
                        {
                            uiCancelToken.ThrowIfCancellationRequested();
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                                // Just eat the exception here, no reason to let an update fail on this and practically speaking, this should never be hit.
                            }
                        }
                        RemoveEmptyDirectories(cachePath);
                    }

                    _appState.LastManifest = latestManifest;
                    _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                    _appState.UIOperation.Message = "Ready to launch!";
                    _appState.UIOperation.Progress = 100;
                    _appState.UIOperation.IsFinished = true;
                    CommandManager.InvalidateRequerySuggested();
                }

            }
            catch (OperationCanceledException) {
                _appState.UIOperation.Message = "Update cancelled.";
                _appState.UIOperation.IsCancelled = true;
                CommandManager.InvalidateRequerySuggested();
            }
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

        private bool CheckInstallPath()
        {
            var arctiumPath = Path.Join(_appState.InstallPath, "Client.exe");
            if (!File.Exists(arctiumPath))
            {
                _logger.LogWarning("Unable to find client files in currently configured install path: {0}", _appState.InstallPath);
                MessageBox.Show(
                    $"The launcher was unable to find a required file in the currently configured client files location: '{_appState.InstallPath}'. Were the files perhaps moved into another folder?\n\nIf so, please select the location where the 'Client.exe' file can be found in the dialog that will open.\n\nIf not, please select the directory where the application should be installed."
                    , "Could not verify client files location", MessageBoxButton.OK, MessageBoxImage.Warning);
                var folderDialog = new VistaFolderBrowserDialog();
                folderDialog.SelectedPath = _appState.InstallPath + "/";
                if (folderDialog.ShowDialog() != true)
                {
                    return false;
                }
                _appState.InstallPath = folderDialog.SelectedPath;
            }
            return true;
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
    }
}
