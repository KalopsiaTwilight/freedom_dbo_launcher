using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.Utilities;
using Google.Apis.Logging;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace FreedomClient.Commands
{
    public class RestoreWoWClientFilesCommand : IRequest
    {
        public bool CompleteReset { get; set; }
    }

    public class RestoreWoWClientFilesCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<RestoreWoWClientFilesCommand>
    {
        public RestoreWoWClientFilesCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger<RestoreWoWClientFilesCommandHandler> logger)
            : base(fileClient, appState, logger)
        {

        }

        public async Task Handle(RestoreWoWClientFilesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting installation restore...");
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Restore WoW Client Files",
                    IsCancellable = true,
                    Message = "Verifiying mandatory files integrity..."
                };
                _appState.LoadState = ApplicationLoadState.VerifyingFiles;
                var manifest = _appState.LastManifest;

                await _fileClient.EnsureFilesInManifest(manifest, _appState.InstallPath!, _appState.UIOperation.CancellationTokenSource.Token);
                StopClientOperation();
                _logger.LogInformation("Installation restored!");

                if (request.CompleteReset)
                {
                    _logger.LogInformation("Removing files not included with install...");
                    _appState.UIOperation.Message = "Removing files not included with install...";
                    foreach (var file in Directory.EnumerateFiles(_appState.InstallPath!, "*", SearchOption.AllDirectories))
                    {
                        _appState.UIOperation.CancellationTokenSource.Token.ThrowIfCancellationRequested();

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
                                _appState.UIOperation.Progress = 100;
                                _appState.UIOperation.Message = "Failed to delete extra files, install files were restored.";
                                _appState.UIOperation.ProgressReport = "";
                                _appState.UIOperation.IsFinished = true;
                                CommandManager.InvalidateRequerySuggested();
                                return;
                            }
                        }
                    }
                    FileSystemUtilities.RemoveEmptyDirectories(_appState.InstallPath!);
                    _logger.LogInformation("Files not included with install removed!");

                    _appState.InstalledPatches.Clear();
                    _appState.InstalledAddons.Clear();
                    _appState.AvailableAddons.ForEach(x => x.IsInstalled = false);
                    _appState.AvailablePatches.ForEach(x => x.IsInstalled = false);
                }
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                _appState.UIOperation.Progress = 100;
                _appState.UIOperation.Message = "Installation succesfully restored!.";
                _appState.UIOperation.ProgressReport = "";
                _appState.UIOperation.IsFinished = true;
                CommandManager.InvalidateRequerySuggested();
            }
            catch(OperationCanceledException) {
                _appState.UIOperation.Message = "Files verification cancelled.";
                _appState.UIOperation.IsCancelled = true;
                CommandManager.InvalidateRequerySuggested();
            }
        }

    }
}
