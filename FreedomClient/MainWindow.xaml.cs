using FreedomClient.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly VerifiedFileClient _fileClient;
        private CancellationToken _downloadToken;
        public MainWindow(VerifiedFileClient fileClient)
        {
            _fileClient = fileClient;
            _fileClient.FileDownloadStarted += OnFileDownloadStart;
            _fileClient.FileDownloadCompleted += OnFileDownloadCompleted;
            _fileClient.FileVerifyStarted += OnFileVerifyStarted;
            _fileClient.FileVerifyCompleted += OnFileVerifyCompleted;
            _fileClient.ClientUpdateCompleted += OnClientUpdateCompleted;
            _fileClient.ExceptionDuringDownload += OnExceptionDuringDownload;
            InitializeComponent();
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            _fileClient.DownloadFiles("http://localhost:3000", "D:/TestDownloadFolder", _downloadToken);
            LaunchButton.IsEnabled = false;
            pgbProgress.Visibility = Visibility.Visible;
        }

        private void OnFileDownloadStart(object? sender, FileDownloadStartedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Downloading {e.FileName}...";
                var keys = e.Manifest.Keys.ToList();
                var progress = Math.Floor((double) keys.IndexOf(e.FileName) / keys.Count * 100);
                pgbProgress.Value= progress;
            });
        }
        private void OnFileDownloadCompleted(object? sender, FileDownloadCompletedEventArgs e)
        {

            Dispatcher.Invoke(() =>
            {
                    txtProgress.Text = $"Downloaded {e.FileName}!";
            });
        }

        private void OnFileVerifyStarted(object? sender, FileVerifyStartedEventArgs e)
        {

            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Verifying {e.FileName}...";
            });
        }

        private void OnFileVerifyCompleted(object? sender, FileVerifyCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Verified {e.FileName}!";
            });
        }

        private void OnClientUpdateCompleted(object? sender, ClientUpdateCompletedEventArgs e)
        {
            pgbProgress.Value = 100;
            txtProgress.Text = "Update complete! WoW Freedom is ready to launch!";
        }
        
        private void OnExceptionDuringDownload(object? sender, ExceptionDuringDownloadEventArgs e)
        {
            LaunchButton.IsEnabled = true;
            pgbProgress.Visibility = Visibility.Hidden;
            txtProgress.Text = "Oops! An error occured during download!";
        }
    }
}
