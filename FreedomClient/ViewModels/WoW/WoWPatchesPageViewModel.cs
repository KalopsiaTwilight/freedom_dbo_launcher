using FreedomClient.DAL;
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

        public WoWPatchesPageViewModel(IServiceProvider serviceProvider)
        {
            _patchesRepository = serviceProvider.GetRequiredService<PatchesRepository>();

            PatchesViewModel = new PatchesViewModel();
            PatchesViewModel.IsLoading = true;
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
