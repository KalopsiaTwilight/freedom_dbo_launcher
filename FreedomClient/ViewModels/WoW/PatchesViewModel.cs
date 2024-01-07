using FreedomClient.Models;
using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace FreedomClient.ViewModels.WoW
{
    [AddINotifyPropertyChangedInterface]
    public class PatchesViewModel
    {
        [AlsoNotifyFor("PatchViews")]
        public List<Patch> Patches { get; set; }

        [AlsoNotifyFor("PatchViews")]
        public ICommand? InstallCommand { get; set; }
        [AlsoNotifyFor("PatchViews")]
        public ICommand? RemoveCommand { get; set; }

        public bool IsLoading { get; set; }
        public IEnumerable<PatchViewModel> PatchViews
        {
            get => Patches.Select(x => new PatchViewModel()
            {
                Patch = x,
                InstallCommand = InstallCommand,
                RemoveCommand = RemoveCommand,
            });
        }

        public PatchesViewModel()
        {
            IsLoading = true;
            Patches = new List<Patch>()
            {
                new Patch() {
                    Author = "KalopsiaTwilight",
                    Description = "Some information about the patch goes here",
                    ImageSrc = "https://placekitten.com/180/120",
                    Title= "My Awesome Patch",
                    Version = "1.0.0"
                },
                new Patch() {
                    Author = "KalopsiaTwilight",
                    Description = "Some information about the patch goes here. You know this one is even longer and more betterer.",
                    ImageSrc = "https://placekitten.com/180/120",
                    Title= "My More Awesome Patch",
                    Version = "2.0.0"
                },
                new Patch() {
                    Author = "KalopsiaTwilight",
                    Description = "Some information about the patch goes here. This is the awesomest patch ever. Once you install this patch you won't be able to go back. Trust me. I make all the best patches. And you're going to want this one in particular.",
                    ImageSrc = "https://placekitten.com/180/120",
                    Title= "My Awesomest Patch Ever (You won't believe this one)",
                    Version = "133.7.420",
                    IsInstalled= true
                }
            };
        }
    }
}
