﻿using FreedomClient.Core;
using FreedomClient.Infrastructure;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly VerifiedFileClient _fileClient;
        private readonly ApplicationState _appState;
        private CancellationToken _downloadToken;
        public MainWindow(VerifiedFileClient fileClient, ApplicationState state)
        {
            _fileClient = fileClient;
            _appState = state;
            _fileClient.FileDownloadStarted += OnFileDownloadStart;
            _fileClient.FileDownloadCompleted += OnFileDownloadCompleted;
            _fileClient.FileVerifyStarted += OnFileVerifyStarted;
            _fileClient.FileVerifyCompleted += OnFileVerifyCompleted;
            _fileClient.ExceptionDuringDownload += OnExceptionDuringDownload;


            InitializeComponent();
            if (state.LoadState == ApplicationLoadState.NotInstalled)
            {
                btnMain.Content = "Install";
            }
            else
            {
                btnMain.Content = "Launch";
                btnMain.IsEnabled = false;
                txtProgress.Text = "Checking for updates...";
                VerifyFiles();
            }
        }

        private async void VerifyFiles()
        {
            var updateReady = await _fileClient.CheckForUpdates(_appState.LastManifest, _downloadToken);
            Dictionary<string, string> manifest;
            if (updateReady)
            {
                await Dispatcher.BeginInvoke(() => { txtProgress.Text = "Updating..."; });
                manifest = await _fileClient.GetManifest(_downloadToken);
            } else
            {
                await Dispatcher.BeginInvoke(() => { txtProgress.Text = "Verifying file integrity..."; });
                manifest = _appState.LastManifest;
            }
            await _fileClient.VerifyFiles(manifest, _appState.InstallPath!, _downloadToken);
            await Dispatcher.BeginInvoke(() =>
            {
                pgbProgress.Value = 100;
                txtProgress.Text = "Ready to launch!";
                btnMain.IsEnabled = true;
            });
        }

        private async void btnMain_Click(object sender, RoutedEventArgs e)
        {
            btnMain.IsEnabled = false;

            if (_appState.LoadState == ApplicationLoadState.NotInstalled)
            {
                var folderDialog = new VistaFolderBrowserDialog();
                if (folderDialog.ShowDialog() != true)
                {
                    btnMain.IsEnabled = true;
                    return;
                }

                _appState.InstallPath = folderDialog.SelectedPath;
                if (!Directory.Exists(_appState.InstallPath))
                {
                    Directory.CreateDirectory(_appState.InstallPath);
                }

                txtProgress.Text = "Downloading manifest...";
                var manifest = await _fileClient.GetManifest(_downloadToken);
                _appState.LastManifest = manifest;
                pgbProgress.Visibility = Visibility.Visible;

                await _fileClient.VerifyFiles(manifest, _appState.InstallPath, _downloadToken);

                pgbProgress.Value = 100;
                txtProgress.Text = "Successfully installed! Client is now ready to launch";
                _appState.LoadState = ApplicationLoadState.ReadyToLaunch;
            }
            // Open arctium launcher otherwise...
        }

        private void btnForums_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://wowfreedom-rp.com/f/");
        }

        private void btnStatus_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://mm.wowfreedom-rp.com/Home/Status");
        }

        private void OnFileDownloadStart(object? sender, FileDownloadStartedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Downloading {e.FileName}...";
                var keys = _appState.LastManifest.Keys.ToList();
                var progress = Math.Floor((double)keys.IndexOf(e.FileName) / keys.Count * 100);
                pgbProgress.Value = progress;
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
                var keys = _appState.LastManifest.Keys.ToList();
                var progress = Math.Floor((double)keys.IndexOf(e.FileName) / keys.Count * 100);
                pgbProgress.Value = progress;
            });
        }

        private void OnFileVerifyCompleted(object? sender, FileVerifyCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtProgress.Text = $"Verified {e.FileName}!";
            });
        }

        private void OnExceptionDuringDownload(object? sender, ExceptionDuringDownloadEventArgs e)
        {
            btnMain.IsEnabled = true;
            pgbProgress.Visibility = Visibility.Hidden;
            txtProgress.Text = "Oops! An error occured during download!";
        }
    }
}
