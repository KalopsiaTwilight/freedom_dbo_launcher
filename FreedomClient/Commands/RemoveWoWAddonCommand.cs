using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.Utilities;
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
    public class RemoveWoWAddonCommand : IRequest
    {
        public Addon? Addon { get; set; }
        public RemoveWoWAddonCommand(Addon? addon)
        {
            Addon = addon;
        }
    }

    public class RemoveWoWAddonCommandHandler : FileClientUIOperationCommandHandler, IRequestHandler<RemoveWoWAddonCommand>
    {
        public RemoveWoWAddonCommandHandler(VerifiedFileClient fileClient, ApplicationState appState, ILogger<RemoveWoWAddonCommandHandler> logger)
            : base(fileClient, appState, logger)
        {
        }

        public async Task Handle(RemoveWoWAddonCommand request, CancellationToken cancellationToken)
        {
            if (request.Addon == null)
            {
                return;
            }
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Removing Addon Files",
                    IsCancellable = true,
                    Message = $"Removing {request.Addon.Title}...",
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

                try
                {
                    foreach (var entry in manifest)
                    {
                        var completePath = Path.Join(addonPath, entry.Key);
                        if (File.Exists(completePath))
                        {
                            File.Delete(completePath);
                        }
                    }
                    FileSystemUtilities.RemoveEmptyDirectories(addonPath);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, null);
                    _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
                    _appState.UIOperation.Progress = 100;
                    _appState.UIOperation.Message = "Failed to delete a file in the addon's manifest. Please manually clean up the files and retry this operation.";
                    _appState.UIOperation.ProgressReport = "";
                    _appState.UIOperation.IsFinished = true;
                    return;
                }

                _appState.InstalledAddons.Remove(request.Addon.Title);
                request.Addon.IsInstalled = false;

                _appState.UIOperation.Progress = 100;
                _appState.UIOperation.Message = "Addon succesfully removed!";
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
