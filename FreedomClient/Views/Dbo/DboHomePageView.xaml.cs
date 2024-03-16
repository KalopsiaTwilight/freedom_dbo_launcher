using FreedomClient.Controls;
using FreedomClient.ViewModels.Dbo;
using Microsoft.Extensions.Logging;
using System.Windows.Controls;

namespace FreedomClient.Views.Dbo
{
    /// <summary>
    /// Interaction logic for WoWHomeView.xaml
    /// </summary>
    public partial class DboHomePageView : Page
    {

        public DboHomePageView(ILoggerFactory loggerFactory, DboHomePageViewModel vm)
        {
            InitializeComponent();
            bgImage.Logger = loggerFactory.CreateLogger<CyclingBackgroundImage>();
            DataContext = vm;
        }

    }
}
