﻿using FreedomClient.ViewModels.WoW;
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
    /// Interaction logic for WoWAddonsPageView.xaml
    /// </summary>
    public partial class WoWAddonsPageView : Page
    {
        public WoWAddonsPageView(WoWAddonsPageViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
