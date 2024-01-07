using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.ViewModels;
using FreedomClient.ViewModels.WoW;
using FreedomClient.Views.WoW;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace FreedomClient.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.MainWindow = this;
        }
        private void MenuGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void HamburgerMenu_OnClickOutsideOfMenu(object sender, MouseEventArgs e)
        {
            (DataContext as MainWindowViewModel)!.IsNavMenuOpen = false;
        }
    }
}
