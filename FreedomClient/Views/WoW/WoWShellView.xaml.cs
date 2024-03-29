﻿using FreedomClient.ViewModels.WoW;
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

namespace FreedomClient.Views.WoW
{
    /// <summary>
    /// Interaction logic for WoWMainPage.xaml
    /// </summary>
    public partial class WoWShellView : Page
    {
        public WoWShellView(WoWShellViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
