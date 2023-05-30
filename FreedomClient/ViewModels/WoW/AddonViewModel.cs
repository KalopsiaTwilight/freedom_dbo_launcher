using FreedomClient.Models;
using System.ComponentModel;
using System.Windows.Input;
using PropertyChanged;

namespace FreedomClient.ViewModels.WoW
{
    [AddINotifyPropertyChangedInterface]
    public class AddonViewModel
    {
        public Addon? Addon { get; set; }

        public ICommand? InstallCommand { get; set; }

        public AddonViewModel()
        {
            Addon = new Addon()
            {
                Author = "KalopsiaTwilight",
                Description = "A blurb about the addon goes here.",
                IsInstalled = false,
                ImageSrc = "https://placekitten.com/180/120",
                Title = "My Addon",
                Version = "1.0.0"
            };
        }
    }
}
