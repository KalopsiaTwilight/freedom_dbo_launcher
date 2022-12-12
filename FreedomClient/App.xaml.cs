using System;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using FreedomClient.Infrastructure;
using FreedomClient.Core;
using Newtonsoft.Json;

namespace FreedomClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ServiceProvider? ServiceProvider { get; private set; }
        public ApplicationState? ApplicationState { get; private set; }
        public ILogger? Logger { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            LoadApplicationState();
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            Logger = ServiceProvider.GetRequiredService<ILogger>();
            SetupExceptionHandling();
            ServiceProvider.GetRequiredService<MainWindow>().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SaveApplicationState();
            base.OnExit(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(ApplicationState!);
            services.AddSingleton(typeof(MainWindow));
            services.AddTransient(typeof(ILogger), typeof(FreedomClientLogger));
            services.AddTransient<VerifiedFileClient>();
            services.AddHttpClient();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logger?.Log(LogLevel.Error, (Exception)e.ExceptionObject, "");
            };
            DispatcherUnhandledException += (s, e) =>
            {
                Logger?.Log(LogLevel.Error, e.Exception, "");
                e.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                if (e.Exception.InnerException != null)
                {
                    Logger?.Log(LogLevel.Error, e.Exception.InnerException, "");
                }
                else
                {
                    Logger?.Log(LogLevel.Error, e.Exception, "");
                }
                e.SetObserved();
            };
        }

        private void LoadApplicationState()
        {
            var appStatePath = GetApplicationStatePath();
            if (File.Exists(appStatePath))
            {
                using (var reader = new StreamReader(appStatePath))
                {
                    var txt = reader.ReadToEnd();
                    ApplicationState = JsonConvert.DeserializeObject<ApplicationState>(txt) ?? new ApplicationState();
                    // TODO: Possible place to perform version upgrades
                    ApplicationState.Version = new ApplicationState().Version;
                    // Check if install path still exists
                    if (!Directory.Exists(ApplicationState.InstallPath))
                    {
                        ApplicationState.InstallPath = null;
                    }
                    ApplicationState.LoadState = string.IsNullOrEmpty(ApplicationState.InstallPath) ? ApplicationLoadState.NotInstalled : ApplicationLoadState.CheckForUpdate;
                }
            }
            else
            {
                ApplicationState = new ApplicationState();
            }

        }

        private void SaveApplicationState()
        {
            var json = JsonConvert.SerializeObject(ApplicationState, Formatting.Indented);
            var appStatePath = GetApplicationStatePath();
            if (!Directory.Exists(appStatePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(appStatePath)!);
            }
            using (var writer = new StreamWriter(appStatePath, false))
            {
                writer.Write(json);
            }
        }

        private string GetApplicationStatePath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appDataFolder, Constants.AppIdentifier, "appstate.json");
        }
    }
}
