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
    public class WoWAddonsPageViewModel: IViewModel
    {
        private AddonsRepository _repository;

        public AddonsViewModel AddonsViewModel { get; set; }

        public WoWAddonsPageViewModel(AddonsRepository repository)
        {
            _repository = repository;

            AddonsViewModel = new AddonsViewModel
            {
                IsLoading = true
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
