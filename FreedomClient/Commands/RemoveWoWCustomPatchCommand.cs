using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FreedomClient.Commands
{
    public class RemoveWoWCustomPatchCommand : IRequest
    {
        public Patch? Patch { get; set; }
        public RemoveWoWCustomPatchCommand(Patch? patch)
        {
            Patch = patch;
        }
    }

    public class RemoveWoWCustomPatchCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<RemoveWoWCustomPatchCommand>
    {
        public RemoveWoWCustomPatchCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger<RemoveWoWCustomPatchCommandHandler> logger)
            : base(fileClient, appState, logger)
        {
        }

        public async Task Handle(RemoveWoWCustomPatchCommand request, CancellationToken cancellationToken)
        {
            if (request.Patch == null)
            {
                return;
            }
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Removing Custom Patch Files",
                    IsCancellable = true,
                    Message = $"Removing {request.Patch.Title}...",
                    Progress = 0
                };

                DownloadManifest manifest;
                try
                {
                    manifest = await _fileClient.GetManifest(request.Patch.Manifest, request.Patch.Signature, _appState.UIOperation.CancellationTokenSource.Token);
                }
                catch (HttpRequestException exc)
                {
                    _logger.LogError(exc, null);
                    _appState.UIOperation.Message = "Unable to connect to Freedom's CDN. Please try again later.";
                    _appState.UIOperation.IsFinished = true;
                    _appState.UIOperation.ProgressReport = "";
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                var patchIdentifier = Path.GetFileNameWithoutExtension(request.Patch.Signature);
                var filesPath = Path.Combine(_appState.InstallPath, "files", patchIdentifier);

                try
                {
                    foreach (var entry in manifest)
                    {
                        var completePath = Path.Join(filesPath, entry.Key);
                        if (File.Exists(completePath))
                        {
                            File.Delete(completePath);
                        }
                    }
                    if (Directory.Exists(filesPath))
                    {
                        FileSystemUtilities.RemoveEmptyDirectories(filesPath);
                        Directory.Delete(filesPath);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, null);
                    _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                    _appState.UIOperation.Progress = 100;
                    _appState.UIOperation.Message = "Failed to delete a file in the patch's manifest. Please manually clean up the files and retry this operation.";
                    _appState.UIOperation.ProgressReport = "";
                    _appState.UIOperation.IsFinished = true;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                try
                {
                    var mappingsFile = Path.Combine(_appState.InstallPath, "mappings", $"{patchIdentifier}.txt");
                    if (File.Exists(mappingsFile))
                    {
                        File.Delete(mappingsFile);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, null);
                    _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                    _appState.UIOperation.Progress = 100;
                    _appState.UIOperation.Message = "Failed to delete the patch mapping files. Please manually delete the file and retry this operation.";
                    _appState.UIOperation.ProgressReport = "";
                    _appState.UIOperation.IsFinished = true;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                _appState.InstalledPatches.RemoveAll(x => x.Title == request.Patch.Title);
                request.Patch.IsInstalled = false;

                _appState.UIOperation.Progress = 100;
                _appState.UIOperation.Message = "Custom patch succesfully removed!";
                _appState.UIOperation.ProgressReport = "";
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                _appState.UIOperation.IsFinished = true;
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
