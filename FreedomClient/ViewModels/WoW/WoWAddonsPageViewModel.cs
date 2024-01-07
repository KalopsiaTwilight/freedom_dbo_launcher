using FreedomClient.Commands;
using FreedomClient.DAL;
using FreedomClient.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FreedomClient.ViewModels.WoW
{
    [AddINotifyPropertyChangedInterface]
    public class WoWAddonsPageViewModel: IViewModel
    {
        private AddonsRepository _repository;
        private ApplicationState _appState;

        public AddonsViewModel AddonsViewModel { get; set; }

        public WoWAddonsPageViewModel(AddonsRepository repository, IMediator mediator, ApplicationState appState)
        {
            _repository = repository;
            _appState = appState;

            AddonsViewModel = new AddonsViewModel
            {
                IsLoading = true,
                InstallCommand = new RelayCommand(
                    (_) => !_appState.UIOperation.IsBusy,
                    (obj) => mediator.Send(new InstallWoWAddonCommand(obj as Addon))
                ),
                RemoveCommand = new RelayCommand(
                    (_) => !_appState.UIOperation.IsBusy,
                    (obj) => mediator.Send(new RemoveWoWAddonCommand(obj as Addon))
                ),
            };
            _repository.GetAddons()
                .ContinueWith(async (x) =>
                {
                    var addons = await x;
                    AddonsViewModel.Addons = addons;
                    AddonsViewModel.IsLoading = false;
                });
        }
    }
}
