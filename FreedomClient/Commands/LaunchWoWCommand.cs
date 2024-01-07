using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.ViewModels;
using Google.Apis.Logging;
using MediatR;
using Microsoft.Extensions.Logging;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FreedomClient.Commands
{
    public class LaunchWoWCommand : IRequest
    {
    }

    public class LaunchWoWCommandHandler : IRequestHandler<LaunchWoWCommand>
    {
        private readonly ApplicationState _appState;
        private readonly MainWindowViewModel _mainWindowVM;
        private readonly ILogger<LaunchWoWCommandHandler> _logger;

        public LaunchWoWCommandHandler(ApplicationState appState, MainWindowViewModel mainWindowVM, ILogger<LaunchWoWCommandHandler> logger)
        {
            _appState = appState;
            _mainWindowVM = mainWindowVM;
            _logger = logger;
        }

        public async Task Handle(LaunchWoWCommand request, CancellationToken cancellationToken)
        {
            Process? process = null;
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Launching WoW",
                    IsCancellable = true,
                    Message = "Launching WoW...",
                    Progress = 0
                };

                var pInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = Path.Combine(_appState.InstallPath!, "Arctium WoW Launcher.exe"),
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                process = Process.Start(pInfo);
                if (process == null)
                {
                    _appState.UIOperation.Message = "Something went wrong launching WoW. Please try again.";
                    _appState.UIOperation.IsCancelled = true;
                    return;
                }
                var linesRead = 0;
                var expectedLines = 297;
                while (!process.HasExited)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    _appState.UIOperation.CancellationTokenSource.Token.ThrowIfCancellationRequested();
                    if (line != null)
                    {
                        linesRead++;
                    }
                    _appState.UIOperation.Progress = (double)linesRead / expectedLines * 100;
                }
                if (process.ExitCode == 0)
                {
                    _mainWindowVM.CloseCommand?.Execute(null);
                }
                else
                {
                    var stdErr = process.StandardError.ReadToEnd();
                    _logger.LogError("Encountered error during launching Arctium Launcher. Following is stderr: {0}{1}", Environment.NewLine, stdErr);

                    _appState.UIOperation.Message = "Something went wrong during the launch. Please contact a dev in #tech-support.";
                    _appState.UIOperation.IsCancelled = true;
                }
            }
            catch (OperationCanceledException)
            {
                process?.Kill(true);
                _appState.UIOperation.Message = "Launch cancelled.";
                _appState.UIOperation.IsCancelled = true;
            }
        }
    }
}
