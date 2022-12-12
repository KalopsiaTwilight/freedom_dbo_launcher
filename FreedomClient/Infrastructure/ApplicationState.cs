using FreedomClient.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FreedomClient.Infrastructure
{
    public enum ApplicationLoadState
    {
        NotInstalled,
        CheckForUpdate,
        VerifyingFiles,
        ReadyToLaunch
    }

    public class ApplicationState
    {
        public event EventHandler<ApplicationLoadState?>? ApplicationLoadStateChanged;
        public ApplicationState()
        {
            LastManifest = new DownloadManifest();
            Version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            LoadState = ApplicationLoadState.NotInstalled;
        }

        public string? InstallPath { get; set; }

        public DownloadManifest LastManifest { get; set; }

        public string Version { get; set; }

        private ApplicationLoadState? _loadState;

        [JsonIgnore]
        public ApplicationLoadState? LoadState { 
            get { return _loadState; } 
            set { _loadState = value; ApplicationLoadStateChanged?.Invoke(this, value); } 
        }
    }
}
