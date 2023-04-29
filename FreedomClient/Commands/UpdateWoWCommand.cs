using FreedomClient.Core;
using FreedomClient.Models;
using Google.Apis.Logging;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FreedomClient.Commands
{
    public class UpdateWoWCommand: IRequest
    {
    }

    public class UpdateWoWCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<UpdateWoWCommand>
    {
        private readonly IMediator _mediator;

        public UpdateWoWCommandHandler(ApplicationState applicationState, ILogger<UpdateWoWCommandHandler> logger, VerifiedFileClient fileClient, IMediator mediator)
            : base(fileClient, applicationState, logger)
        {
            _mediator = mediator;
        }

        public async Task Handle(UpdateWoWCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    IsCancellable = true,
                    Name = "Updating WoW",
                    Message = "Checking for updates...",
                    Progress = 0
                };
                var uiCancelToken = _appState.UIOperation.CancellationTokenSource.Token;
                _logger.LogInformation("Checking for client files updates...");

                // Checkout latest manifest
                DownloadManifest latestManifest;
                try
                {
                    latestManifest = await _fileClient.GetManifest(uiCancelToken);
                }
                catch (HttpRequestException exc)
                {
                    _appState.UIOperation.Message = "Unable to connect to Freedom's CDN to check for updates." + Environment.NewLine +
                        "You might not be able to log in.";
                    _appState.UIOperation.Progress = 100;
                    _appState.UIOperation.IsFinished = true;
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
                }
                else
                {
                    // Update needed
                    _logger.LogInformation("Starting client files update...");
                    // Get update manifest
                    var patchManifest = latestManifest.CreatePatchManifestFrom(_appState.LastManifest);
                    _appState.LoadState = ApplicationLoadState.CheckForUpdate;
                    _appState.UIOperation.Message = "Updating...";
                    await _fileClient.VerifyFiles(patchManifest, _appState.InstallPath, uiCancelToken);
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
                }

                if (!CheckRequiredFilesExist(_appState.LastManifest))
                {
                    _logger.LogInformation("Found missing files, starting restore..");
                    _appState.UIOperation.Progress = 0;
                    _appState.UIOperation.Message = "Restoring missing files...";

                    await _mediator.Send(new RestoreWoWClientFilesCommand() { CompleteReset = false });
                }
            }
            catch (OperationCanceledException) {
                _appState.UIOperation.Message = "Update cancelled.";
                _appState.UIOperation.IsCancelled = true;
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
