
using FreedomClient.ViewModels.Dbo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;

namespace FreedomClient.Views.Dbo
{
    /// <summary>
    /// Interaction logic for WoWSettingsPage.xaml
    /// </summary>
    public partial class DboSettingsPageView : Page
    {
        //private readonly ApplicationState _applicationState;
        public DboSettingsPageView(DboSettingsPageViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
