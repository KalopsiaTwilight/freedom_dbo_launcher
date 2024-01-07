using FreedomClient.Core;
using FreedomClient.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FreedomClient.Commands
{
    public class InstallWoWCustomPatchCommand : IRequest
    {
        public Patch? Patch { get; set; }
        public InstallWoWCustomPatchCommand(Patch? patch)
        {
            Patch = patch;
        }
    }

    public class InstallWoWCustomPatchCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<InstallWoWCustomPatchCommand>
    {
        public InstallWoWCustomPatchCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger<InstallWoWCustomPatchCommandHandler> logger)
            : base(fileClient, appState, logger)
        {
        }

        public async Task Handle(InstallWoWCustomPatchCommand request, CancellationToken cancellationToken)
        {
            if (request.Patch == null)
            {
                return;
            }
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Downloading Custom Patch Files",
                    IsCancellable = true,
                    Message = $"Installing {request.Patch.Title}...",
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
                    return;
                }

                var patchIdentifier = Path.GetFileNameWithoutExtension(request.Patch.Signature);

                var filesPath = Path.Combine(_appState.InstallPath, "files", patchIdentifier);
                var driveInfo = new DriveInfo(filesPath);
                var totalDownloadSize = manifest.Sum(x => x.Value.FileSize);
                if (driveInfo.AvailableFreeSpace < totalDownloadSize)
                {
                    _appState.UIOperation.Message = $"Not enough free space on drive. {BytesToString(totalDownloadSize)} is required.";
                    return;
                }
                
                if (!Directory.Exists(filesPath))
                {
                    Directory.CreateDirectory(filesPath);
                }

                await _fileClient.EnsureFilesInManifest(manifest, filesPath, _appState.UIOperation.CancellationTokenSource.Token);

                var mappingsFile = Path.Combine(_appState.InstallPath, "mappings", $"{patchIdentifier}.txt");
                var mappingsLines = manifest.Keys.Select(x => $"{request.Patch.ListFileMapping[x]};{patchIdentifier}/{x}");
                File.WriteAllLines(mappingsFile, mappingsLines);

                _appState.InstalledPatches.Add(request.Patch);
                request.Patch.IsInstalled = true;

                _appState.UIOperation.Progress = 100;
                _appState.UIOperation.Message = $"Successfully installed custom patch: {request.Patch.Title}!";
                _appState.UIOperation.ProgressReport = "";
                _appState.UIOperation.IsFinished = true;
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                CommandManager.InvalidateRequerySuggested();
            }
            catch(OperationCanceledException)
            {
                _appState.UIOperation.Message = "Installation cancelled.";
                _appState.UIOperation.IsCancelled = true;
            }
        }
    }
}
