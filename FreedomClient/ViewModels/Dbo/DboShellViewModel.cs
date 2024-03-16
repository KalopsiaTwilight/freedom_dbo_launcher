using FreedomClient.Commands;
using FreedomClient.Models;
using FreedomClient.Views.Dbo;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged;
using System;
using System.Windows.Input;

namespace FreedomClient.ViewModels.Dbo
{
    [AddINotifyPropertyChangedInterface]
    public class DboShellViewModel : IViewModel
    {
        public ICommand? NavigateFrameCommand { get; set; }
        public ICommand? InstallCommand { get; set; }
        public ICommand? LaunchCommand { get; set; }
        public ICommand? CancelOperationCommand { get; set; }
        public bool IsInstalled { get; set; }
        public ApplicationState ApplicationState { get; set; }

        [AlsoNotifyFor("CurrentFrameType", "CurrentFrameTitle")]
        public object CurrentFrame { get; set; }
        public Type CurrentFrameType {  get => CurrentFrame.GetType(); }
        public string CurrentFrameTitle { get
            {
                switch (CurrentFrameType.Name)
                {
                    case nameof(DboHomePageView): return "Home";
                    case nameof(DboSettingsPageViewModel): return "Settings";
                    default: return "Unknown";
                }
            }
        }

        private readonly IServiceProvider _serviceProvider;

        public DboShellViewModel(IServiceProvider serviceProvider) {
            CurrentFrame = serviceProvider.GetRequiredService<DboHomePageView>();
            ApplicationState = serviceProvider.GetRequiredService<ApplicationState>();
            IsInstalled = !string.IsNullOrEmpty(ApplicationState.InstallPath);
            _serviceProvider = serviceProvider;

            var mediator = serviceProvider.GetRequiredService<IMediator>();

            mediator.Send(new UpdateDboCommand());

            CancelOperationCommand = new RelayCommand(
                (_) => ApplicationState.UIOperation.IsCancellable
                    && !ApplicationState.UIOperation.IsFinished
                    && !ApplicationState.UIOperation.IsCancelled,
                (_) =>
                {
                    ApplicationState.UIOperation.CancellationTokenSource.Cancel();
                });

            NavigateFrameCommand = new RelayCommand((x) => !CurrentFrameType.Equals(x), (x) =>
            {
                if (x is Type frameType)
                {
                    CurrentFrame = _serviceProvider.GetRequiredService(frameType);
                }    
            });

            InstallCommand = new RelayCommand(
                (_) => ApplicationState.LoadState == ApplicationLoadState.NotInstalled,
                (_) =>
                {
                    mediator.Send(new DownloadDboClientFilesCommand(this));
                }
            );

            LaunchCommand = new RelayCommand(
                (_) => !ApplicationState.UIOperation.IsBusy,
                (_) =>
                {
                    mediator.Send(new LaunchDboCommand());
                }
            );
        }
    }
}
