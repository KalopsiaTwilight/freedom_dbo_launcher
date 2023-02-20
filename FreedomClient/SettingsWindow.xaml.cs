using FreedomClient.Core;
using FreedomClient.Infrastructure;
using Ookii.Dialogs.Wpf;
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
using System.Windows.Shapes;

namespace FreedomClient
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ApplicationState _applicationState;
        private readonly MainWindow _mainWindow;
        public SettingsWindow(MainWindow mainWindow, ApplicationState appState)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _applicationState = appState;
            txtInstallPath.Text = _applicationState.InstallPath;
            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            txtLogPath.Text = System.IO.Path.Join(localDataPath, Constants.AppIdentifier);
        }

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnInstallPath_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new VistaFolderBrowserDialog();
            if (folderDialog.ShowDialog() != true)
            {
                return;
            }

            _applicationState.InstallPath = folderDialog.SelectedPath;
            if (_applicationState.LoadState == ApplicationLoadState.ReadyToLaunch)
            {
                _mainWindow.VerifyInstall();
            }
            Close();
        }

        private void btnLogDir_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtLogPath.Text);
            Close();
        }

        private void ResetInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_applicationState.LoadState == ApplicationLoadState.ReadyToLaunch)
            {
                _mainWindow.VerifyInstall(true);
            }
            Close();
        }
    }
}
