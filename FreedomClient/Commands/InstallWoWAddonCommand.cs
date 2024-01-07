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
    public class InstallWoWAddonCommand : IRequest
    {
        public Addon? Addon { get; set; }
        public InstallWoWAddonCommand(Addon? addon)
        {
            Addon = addon;
        }
    }

    public class InstallWoWAddonCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<InstallWoWAddonCommand>
    {
        public InstallWoWAddonCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger<InstallWoWAddonCommandHandler> logger)
            : base(fileClient, appState, logger)
        {
        }

        public async Task Handle(InstallWoWAddonCommand request, CancellationToken cancellationToken)
        {
            if (request.Addon == null)
            {
                return;
            }
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Downloading Addon Files",
                    IsCancellable = true,
                    Message = $"Installing {request.Addon.Title}...",
                    Progress = 0
                };

                DownloadManifest manifest;
                try
                {
                    manifest = await _fileClient.GetManifest(request.Addon.Manifest, request.Addon.Signature, _appState.UIOperation.CancellationTokenSource.Token);
                }
                catch (HttpRequestException exc)
                {
                    _logger.LogError(exc, null);
                    _appState.UIOperation.Message = "Unable to connect to Freedom's CDN. Please try again later.";
                    return;
                }

                var addonPath = Path.Join(_appState.InstallPath, "_retail_/Interface/Addons");

                var driveInfo = new DriveInfo(addonPath);
                var totalDownloadSize = manifest.Sum(x => x.Value.FileSize);
                if (driveInfo.AvailableFreeSpace < totalDownloadSize)
                {
                    _appState.UIOperation.Message = $"Not enough free space on drive. {BytesToString(totalDownloadSize)} is required.";
                    return;
                }

                await _fileClient.EnsureFilesInManifest(manifest, addonPath, _appState.UIOperation.CancellationTokenSource.Token);

                _appState.InstalledAddons.Add(request.Addon);
                request.Addon.IsInstalled = true;

                _appState.UIOperation.Progress = 100;
                _appState.UIOperation.Message = $"Successfully installed addon: {request.Addon.Title}!";
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
