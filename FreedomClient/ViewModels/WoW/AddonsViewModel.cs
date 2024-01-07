using FreedomClient.Models;
using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace FreedomClient.ViewModels.WoW
{
    [AddINotifyPropertyChangedInterface]
    public class AddonsViewModel
    {
        [AlsoNotifyFor("AddonViews")]
        public List<Addon> Addons { get; set; }

        [AlsoNotifyFor("AddonViews")]
        public ICommand? InstallCommand { get; set; }

        [AlsoNotifyFor("AddonViews")]
        public ICommand? RemoveCommand { get; set; }

        public bool IsLoading { get; set; }
        public IEnumerable<AddonViewModel> AddonViews
        {
            get => Addons.Select(x => new AddonViewModel()
            {
                Addon = x,
                InstallCommand = InstallCommand,
                RemoveCommand = RemoveCommand,
            });
        }

        public AddonsViewModel()
        {
            IsLoading = false;
            Addons = new List<Addon>()
            {
                new Addon() {
                    Author = "KalopsiaTwilight",
                    Description = "Some information about the addon goes here",
                    ImageSrc = "https://placekitten.com/180/120",
                    Title= "My Awesome Addon",
                    Version = "1.0.0"
                },
                new Addon() {
                    Author = "KalopsiaTwilight",
                    Description = "Some information about the addon goes here. You know this one is even longer and more betterer.",
                    ImageSrc = "https://placekitten.com/180/120",
                    Title= "My More Awesome Addon",
                    Version = "2.0.0"
                },
                new Addon() {
                    Author = "KalopsiaTwilight",
                    Description = "Some information about the addon goes here. This is the awesomest addon ever. Once you install this addon you won't be able to go back. Trust me. I make all the best addons. And you're going to want this one in particular.",
                    ImageSrc = "https://placekitten.com/180/120",
                    Title= "My Awesomest Addon Ever (You won't believe this one)",
                    Version = "133.7.420",
                    IsInstalled= true
                }
            };
        }
    }
}
