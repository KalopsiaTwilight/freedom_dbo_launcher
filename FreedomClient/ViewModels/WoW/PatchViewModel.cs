using FreedomClient.Models;
using System.ComponentModel;
using System.Windows.Input;
using PropertyChanged;

namespace FreedomClient.ViewModels.WoW
{
    [AddINotifyPropertyChangedInterface]
    public class PatchViewModel
    {
        public Patch? Patch { get; set; }

        public ICommand? InstallCommand { get; set; }

        public PatchViewModel()
        {
            Patch = new Patch()
            {
                Author = "KalopsiaTwilight",
                Description = "A blurb about the patch goes here.",
                IsInstalled = false,
                ImageSrc = "https://placekitten.com/180/120",
                Title = "My Patch",
                Version = "1.0.0"
            };
        }
    }
}
