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

        public AddonsViewModel AddonsViewModel { get; set; }

        public WoWAddonsPageViewModel(AddonsRepository repository, IMediator mediator)
        {
            _repository = repository;

            AddonsViewModel = new AddonsViewModel
            {
                IsLoading = true,
                InstallCommand = new RelayCommand(
                    (obj) => true,
                    (obj) => mediator.Send(new InstallWoWAddonCommand(obj as Addon))
                ),
                RemoveCommand = new RelayCommand(
                    (obj) => true,
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
