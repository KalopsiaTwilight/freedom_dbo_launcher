using FreedomClient.Models;
using FreedomClient.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FreedomClient.Commands
{
    public class LaunchDboCommand : IRequest
    {
    }

    public class LaunchDboCommandHandler : IRequestHandler<LaunchDboCommand>
    {
        private readonly ApplicationState _appState;
        private readonly MainWindowViewModel _mainWindowVM;
        private readonly ILogger<LaunchDboCommandHandler> _logger;

        public LaunchDboCommandHandler(ApplicationState appState, MainWindowViewModel mainWindowVM, ILogger<LaunchDboCommandHandler> logger)
        {
            _appState = appState;
            _mainWindowVM = mainWindowVM;
            _logger = logger;
        }

        public async Task Handle(LaunchDboCommand request, CancellationToken cancellationToken)
        {
            Process? process = null;
            try
            {
                _appState.UIOperation = new UIOperation()
                {
                    Name = "Launching Dbo",
                    IsCancellable = true,
                    Message = "Launching Dbo...",
                    Progress = 0
                };

                var pInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = _appState.InstallPath,
                    UseShellExecute = false,
                    FileName = Path.Combine(_appState.InstallPath!, "Client.exe"),
                };
                process = Process.Start(pInfo);
                if (process == null)
                {
                    _appState.UIOperation.Message = "Something went wrong launching DBOG. Please try again.";
                    _appState.UIOperation.IsCancelled = true;
                    return;
                }
                _mainWindowVM.CloseCommand?.Execute(null);
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
