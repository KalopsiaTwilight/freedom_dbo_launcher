using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.Views;
using FreedomClient.Views.WoW;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FreedomClient.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel: IViewModel
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly HttpClient _httpClient;
        private readonly ApplicationState _appState;

        public ICommand? MinimizeCommand { get; set; }
        public ICommand? CloseCommand { get; set; }
        public object CurrentFrame { get; set; }
        public MainWindow? MainWindow { get; set; }

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<MainWindowViewModel>>();
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            _appState = serviceProvider.GetRequiredService<ApplicationState>();

            MinimizeCommand = new RelayCommand((_) => true, (_) => { if (MainWindow != null) { MainWindow.WindowState = WindowState.Minimized; } });
            CloseCommand = new RelayCommand((_) => true, (_) => { MainWindow?.Close(); });

            CurrentFrame = serviceProvider.GetRequiredService<WoWShellView>();

            TestLatestVersion();
        }

        private async void TestLatestVersion()
        {
            _logger.LogInformation("Checking for launcher updates...");
            try
            {
                var resp = await _httpClient.GetAsync(Constants.CdnUrl + "/latestClientVersion.txt");
                if (resp.IsSuccessStatusCode)
                {
                    var versionTxt = await resp.Content.ReadAsStringAsync();
                    if (Version.Parse(_appState.Version) < Version.Parse(versionTxt))
                    {
                        _logger.LogInformation($"A launcher update is available, new version: {versionTxt}.");
                        var result = MessageBox.Show("A new launcher version is available, would you like to download it now?", "New version available", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            var pStart = new ProcessStartInfo()
                            {
                                UseShellExecute = true,
                                FileName = "https://github.com/KalopsiaTwilight/freedom_client/releases/"
                            };
                            Process.Start(pStart);
                            MainWindow?.Close();
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Launcher is up to date!");
                    }
                }
            }
            catch (HttpRequestException exc)
            {
                _logger.LogError(exc, null);
            }
        }

    }
}
