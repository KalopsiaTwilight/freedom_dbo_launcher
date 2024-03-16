using FreedomClient.ViewModels.Dbo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FreedomClient.Views.Dbo
{
    /// <summary>
    /// Interaction logic for WoWMainPage.xaml
    /// </summary>
    public partial class DboShellView : Page
    {
        public DboShellView(DboShellViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
