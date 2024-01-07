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

namespace FreedomClient.ViewModels.WoW
{
    [AddINotifyPropertyChangedInterface]
    public class WoWPatchesPageViewModel: IViewModel
    {
        private PatchesRepository _patchesRepository;

        public PatchesViewModel PatchesViewModel { get; set; }

        public WoWPatchesPageViewModel(PatchesRepository repository, IMediator mediator)
        {
            _patchesRepository = repository;

            PatchesViewModel = new PatchesViewModel
            {
                IsLoading = true,
                InstallCommand = new RelayCommand(
                (obj) => true,
                    (obj) => mediator.Send(new InstallWoWCustomPatchCommand(obj as Patch))
                ),
                RemoveCommand = new RelayCommand(
                (obj) => true,
                    (obj) => mediator.Send(new RemoveWoWCustomPatchCommand(obj as Patch))
                ),
            };
            _patchesRepository.GetPatches()
                .ContinueWith(async (x) =>
                {
                    var patches = await x; 
                    PatchesViewModel.Patches = patches; 
                    PatchesViewModel.IsLoading = false;
                });
        }
    }
}
