using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using FreedomClient.Core;

namespace FreedomClient.Infrastructure
{
    public class FreedomClientLogger : ILogger
    {
        private readonly string _outputPath;
        public FreedomClientLogger()
        {
            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataPath = Path.Join(localDataPath, Constants.AppIdentifier);
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            _outputPath = Path.Join(appDataPath, "log.txt");
        }
        
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }


        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string? message;
            if (exception == null)
            {
                message = state?.ToString();
            }
            else
            {
                message = exception.Message;
            }
            if (string.IsNullOrEmpty(message))
            {
                message = formatter(state, exception);
            }
            if (string.IsNullOrEmpty(message))
            {
                message = $"Unknown error. StateType: {typeof(TState).Name}. State: {state}.";
            }
            WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {logLevel.ToString().ToUpper()}: {message}");
        }
        private void WriteLine(string line)
        {
            using(var stream = File.OpenWrite(_outputPath))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(line);
                }
            }
        }
    }
}
