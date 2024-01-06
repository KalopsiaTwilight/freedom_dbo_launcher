using System;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using FreedomClient.Models;
using FreedomClient.Core;
using Newtonsoft.Json;
using Serilog;

using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Reflection;
using System.Linq;
using System.Windows.Controls;
using FreedomClient.DAL;
using System.Collections.Generic;
using FreedomClient.ViewModels;
using System.Net.Http.Headers;
using FreedomClient.Views;
using FreedomClient.Migrations;

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
            Logger = ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<App>();
            Logger.LogInformation($"Launcher starting up... Running version: {ApplicationState!.Version}!");
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

            var singletonTypes = new List<Type>();

            singletonTypes.AddRange(Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsSubclassOf(typeof(Page))));
            singletonTypes.AddRange(Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IViewModel))));
            foreach (var type in singletonTypes)
            {
                services.AddSingleton(type);
            }

            var repositories = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IRepository)));
            foreach(var repository in repositories)
            {
                services.AddTransient(repository);
            }

            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataPath = Path.Join(localDataPath, Constants.AppIdentifier);
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            services.AddLogging(lb => lb.AddSerilog(new LoggerConfiguration()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    Path.Join(appDataPath, "log.txt"),
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ThreadId}|{ThreadName}] {SourceContext} {Message:lj} {NewLine}{Exception}",
                    fileSizeLimitBytes: 1000 * 1024, rollOnFileSizeLimit: true, retainedFileCountLimit: 3)
                .CreateLogger())
            );

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<App>());

            services.AddTransient<VerifiedFileClient>();
            services.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName, (opt) =>
            {
                opt.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WoWFreedomLauncher", null));
            });
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
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new DownloadSourceJsonConverter());
                    var txt = reader.ReadToEnd();
                    try
                    {
                        ApplicationState = JsonConvert.DeserializeObject<ApplicationState>(txt, settings);
                    } catch { }
                    ApplicationState ??= new ApplicationState();
                    
                    
                    if (ApplicationState.Version != new ApplicationState().Version)
                    {
                        AppStateMigrator.Migrate(ApplicationState);
                    }

                    // Check if install path still exists
                    if (!Directory.Exists(ApplicationState.InstallPath))
                    {
                        ApplicationState.InstallPath = string.Empty;
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
            var settings = new JsonSerializerSettings();
            settings.Formatting= Formatting.Indented;
            settings.Converters.Add(new DownloadSourceJsonConverter());
            var json = JsonConvert.SerializeObject(ApplicationState, settings);
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
