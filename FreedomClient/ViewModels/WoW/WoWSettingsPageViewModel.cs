using FreedomClient.Commands;
using FreedomClient.Core;
using FreedomClient.Models;
using MediatR;
using Ookii.Dialogs.Wpf;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace FreedomClient.ViewModels.WoW
{

    [AddINotifyPropertyChangedInterface]
    public class WoWSettingsPageViewModel: IViewModel
    {
        private readonly ApplicationState _appState;

        public ICommand? ResetInstallCommand { get; set; }

        public ICommand? CopyLogDirCommand { get; set; }

        public ICommand? ChangeInstallPathCommand { get; set; }

        public string InstallPath { get; set; }

        public string LogPath { get; set; }

        public WoWSettingsPageViewModel(ApplicationState appState, IMediator mediator)
        {
            _appState = appState;

            InstallPath = appState.InstallPath ?? string.Empty;
            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            LogPath = System.IO.Path.Join(localDataPath, Constants.AppIdentifier);
            CopyLogDirCommand = new RelayCommand((_) => true, (_) =>
            {
                Clipboard.SetText(LogPath);
            });
            ResetInstallCommand = new RelayCommand((_) => !_appState.UIOperation.IsBusy,
                (_) =>
                {
                    mediator.Send(new RestoreWoWClientFilesCommand() { CompleteReset = false });
                });
            ChangeInstallPathCommand = new RelayCommand((_) => !_appState.UIOperation.IsBusy,
                (_) =>
                {
                    var folderDialog = new VistaFolderBrowserDialog();
                    if (folderDialog.ShowDialog() != true)
                    {
                        return;
                    }

                    InstallPath = folderDialog.SelectedPath;
                    _appState.InstallPath = folderDialog.SelectedPath;
                    mediator.Send(new RestoreWoWClientFilesCommand() { CompleteReset = true });
                });
        }
    }
}
