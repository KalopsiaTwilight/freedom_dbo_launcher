using FreedomClient.ViewModels.WoW;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;

namespace FreedomClient.Views.WoW
{
    /// <summary>
    /// Interaction logic for WoWPatchesPage.xaml
    /// </summary>
    public partial class WoWPatchesPageView : Page
    {
        public WoWPatchesPageView(WoWPatchesPageViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;
        }
    }
}
