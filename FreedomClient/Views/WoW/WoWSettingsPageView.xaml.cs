using FreedomClient.ViewModels.WoW;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;

namespace FreedomClient.Views.WoW
{
    /// <summary>
    /// Interaction logic for WoWSettingsPage.xaml
    /// </summary>
    public partial class WoWSettingsPageView : Page
    {
        //private readonly ApplicationState _applicationState;
        public WoWSettingsPageView(WoWSettingsPageViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
