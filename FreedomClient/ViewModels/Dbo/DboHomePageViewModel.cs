using FreedomClient.Controls;
using FreedomClient.Core;
using FreedomClient.Models;
using FreedomClient.Views.WoW;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace FreedomClient.ViewModels.Dbo
{
    public enum ServerStatus
    {
        Up,
        StartingUp,
        Down,
        Unknown
    }

    [AddINotifyPropertyChangedInterface]
    public class DboHomePageViewModel : IViewModel
    {
        private readonly ApplicationState _appState;
        private readonly ILogger<DboHomePageViewModel> _logger;
        private readonly HttpClient _httpClient;

        //private Timer _serverStatusTimer;
        public ObservableCollection<string> LauncherImages { get; set; }
        public ServerStatus ServerStatus { get; set; }
        public ICommand? GoToForumsCommand { get; set; }


        public DboHomePageViewModel(ApplicationState state, ILogger<DboHomePageViewModel> logger, HttpClient httpClient)
        {
            _appState= state;
            _logger = logger;
            _httpClient = httpClient;

            ServerStatus = ServerStatus.Unknown;
            //_serverStatusTimer = new Timer(new TimerCallback(UpdateServerStatus), null, 0, 10000);

            LauncherImages = new ObservableCollection<string>(_appState.LauncherImages.Where(x => File.Exists(x)).ToList());
            GoToForumsCommand = new RelayCommand((_) => true, (_) =>
            {
                var pStart = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = "https://wowfreedom-rp.com/f/"
                };
                Process.Start(pStart);
            });

            UpdateLauncherImages();
        }


        private async void UpdateServerStatus(object? state)
        {
            var debugTimer = Stopwatch.StartNew();
            try
            {
                var resp = await _httpClient.PostAsync(Constants.MinimanagerUrl + "/Data/StatusLinePartial", null); debugTimer.Stop();
                Debug.WriteLine($"Received status line partial response in {debugTimer.ElapsedMilliseconds} ms.");
                ServerStatus = ServerStatus.Unknown;
                if (resp.IsSuccessStatusCode)
                {
                    var statusText = await resp.Content.ReadAsStringAsync();
                    var match = Regex.Match(statusText, ".*status-(\\w+)");
                    if (match.Success)
                    {
                        switch (match.Groups[1].Value)
                        {
                            case "good": ServerStatus = ServerStatus.Up; break;
                            case "loading": ServerStatus = ServerStatus.StartingUp; break;
                            case "bad": ServerStatus = ServerStatus.Down; break;
                        }
                    }

                }
            }
            catch (Exception e)
            {
                ServerStatus = ServerStatus.Unknown;
                _logger.LogError(e, null);
                return;
            }   
        }

        private async void UpdateLauncherImages()
        {
            _logger.LogInformation("Updating launcher images...");
            var credential = GoogleCredential
                .FromStream(new EmbeddedFileProvider(Assembly.GetEntryAssembly()!,"FreedomClient").GetFileInfo(Constants.GoogleCredentialsJsonPath).CreateReadStream())
                .CreateScoped(DriveService.Scope.Drive);
            var service = new DriveService(new BaseClientService.Initializer()
            {
                ApplicationName = Constants.AppIdentifier,
                HttpClientInitializer = credential
            });
            var listFilesRequest = service.Files.List();
            listFilesRequest.SupportsAllDrives = true;
            listFilesRequest.IncludeItemsFromAllDrives = true;
            listFilesRequest.Q = $"'{Constants.LauncherImagesDriveFolderId}' in parents";
            var listFilesResponse = await listFilesRequest.ExecuteAsync();
            if (listFilesResponse == null)
            {
                _logger.LogError("Unable to download images from google drive");
                return;
            }
            var imageCollection = new List<string>();
            var launcherImagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.AppIdentifier, "launcherImages");
            if (!Directory.Exists(launcherImagePath))
            {
                Directory.CreateDirectory(launcherImagePath);
            }
            foreach (var file in listFilesResponse.Files)
            {
                var outputPath = Path.Combine(launcherImagePath, file.Name);
                if (!File.Exists(outputPath))
                {
                    var downloadFileRequest = service.Files.Get(file.Id);
                    downloadFileRequest.AcknowledgeAbuse = true;
                    using (var fileStream = File.Create(outputPath))
                    {
                        var progress = await downloadFileRequest.DownloadAsync(fileStream);

                        if (progress.BytesDownloaded == 0)
                        {
                            throw new InvalidDataException();
                        }
                    }
                }
                imageCollection.Add(outputPath);
            }
            LauncherImages = new ObservableCollection<string>(imageCollection.OrderBy(x => Path.GetFileName(x)).ToList());
            // Clean up old images
            foreach (var img in _appState.LauncherImages)
            {
                if (!imageCollection.Contains(img))
                {
                    File.Delete(img);
                }
            }
            _appState.LauncherImages = imageCollection;
            _logger.LogInformation("Launcher images updated!");
        }
    }
}
