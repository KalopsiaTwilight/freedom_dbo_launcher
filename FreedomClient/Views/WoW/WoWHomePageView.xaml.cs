using FreedomClient.Controls;
using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.ViewModels.WoW;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FreedomClient.Views.WoW
{
    /// <summary>
    /// Interaction logic for WoWHomeView.xaml
    /// </summary>
    public partial class WoWHomePageView : Page
    {

        public WoWHomePageView(ILoggerFactory loggerFactory, WoWHomePageViewModel vm)
        {
            InitializeComponent();
            bgImage.Logger = loggerFactory.CreateLogger<CyclingBackgroundImage>();
            DataContext = vm;
        }

    }
}
