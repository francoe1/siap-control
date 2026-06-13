using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;

namespace SiapControl.Views
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<UserModel> _users = new();
        private bool _databaseReady;

        public MainWindow()
        {
            InitializeComponent();
            Title = $"SiapControl v{GetApplicationVersion()}";

            UsersGrid.ItemsSource = _users;
            Loaded += OnLoaded;

            CheckUpdateButton.Click += CheckForUpdatesManually;
            AddUserButton.Click += AddUser;
            UpdateButton.Click += UpdateApp;
            ModulesButton.Click += ShowModules;
            UsersGrid.MouseDoubleClick += EditUser;
            UsersGrid.SelectionChanged += OnSelectionChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            var progress = new Progress<DatabaseInitializationProgress>(ShowStartupProgress);

            try
            {
                SetControlsEnabled(false);
                await Database.ConnectAsync(progress);
                _databaseReady = true;
                StartupStatusPanel.Visibility = Visibility.Collapsed;
                SetControlsEnabled(true);
                LoadUsers();
                _ = CheckForUpdatesAsync(showNoUpdateMessage: false);
            }
            catch (Exception ex)
            {
                StartupStatusText.Text = "No se pudo abrir la base de datos.";
                MessageBox.Show($"No se pudo abrir la base de datos.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadUsers()
        {
            _users.Clear();
            foreach (UserModel user in Database.Users.FindAll())
            {
                _users.Add(user);
            }
            ModulesButton.IsEnabled = UsersGrid.SelectedItem != null;
        }

        private void ShowStartupProgress(DatabaseInitializationProgress progress)
        {
            StartupStatusText.Text = progress.Message;
            StartupProgressBar.Value = progress.Percentage;
        }

        private async void CheckForUpdatesManually(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync(showNoUpdateMessage: true);
        }

        private async System.Threading.Tasks.Task CheckForUpdatesAsync(bool showNoUpdateMessage)
        {
            bool wasEnabled = CheckUpdateButton.IsEnabled;

            try
            {
                CheckUpdateButton.IsEnabled = false;
                var updateService = new GitHubUpdateService();
                GitHubUpdate? update = await updateService.CheckForUpdateAsync();
                if (update == null)
                {
                    if (showNoUpdateMessage)
                    {
                        MessageBox.Show(this, "Ya tenés la última versión disponible.", "Sin actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    this,
                    $"Hay una nueva versión disponible: {update.Version}.\n¿Querés descargarla e instalarla ahora?",
                    "Actualización disponible",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                SetControlsEnabled(false);
                StartupStatusText.Text = "Descargando actualización...";
                StartupProgressBar.IsIndeterminate = true;
                StartupStatusPanel.Visibility = Visibility.Visible;

                await updateService.DownloadAndInstallAsync(update);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                if (showNoUpdateMessage)
                {
                    MessageBox.Show(this, $"No se pudo buscar actualizaciones.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                if (_databaseReady)
                {
                    CheckUpdateButton.IsEnabled = wasEnabled;
                }
            }
        }

        private static string GetApplicationVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            return version.ToString();
        }

        private void SetControlsEnabled(bool isEnabled)
        {
            UsersGrid.IsEnabled = isEnabled;
            CheckUpdateButton.IsEnabled = isEnabled;
            AddUserButton.IsEnabled = isEnabled;
            UpdateButton.IsEnabled = isEnabled;
            ModulesButton.IsEnabled = isEnabled && UsersGrid.SelectedItem != null;
        }

        private void AddUser(object sender, RoutedEventArgs e)
        {
            var dialog = new UserWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var model = new UserModel { User = dialog.UserName, Path = dialog.SiapPath };
                Database.Users.Insert(model);
                LoadUsers();
            }
        }

        private void EditUser(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UsersGrid.SelectedItem is not UserModel user)
                return;

            var dialog = new UserWindow { Owner = this, UserName = user.User, SiapPath = user.Path };
            if (dialog.ShowDialog() == true)
            {
                user.User = dialog.UserName;
                user.Path = dialog.SiapPath;
                Database.Users.Update(user);
                LoadUsers();
            }
        }

        private void ShowModules(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not UserModel user)
                return;

            var win = new UserModulesWindow(user.Id) { Owner = this };
            win.ShowDialog();
        }

        private void UpdateApp(object sender, RoutedEventArgs e)
        {
            var open = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Ejecutables|*.exe"
            };

            if (open.ShowDialog() == true)
            {
                var setup = new SetupReader(open.FileName);
                if (!setup.Open())
                {
                    return;
                }

                var win = new InstallerWindow(setup) { Owner = this };
                win.ShowDialog();
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModulesButton.IsEnabled = _databaseReady && UsersGrid.SelectedItem != null;
        }
    }
}
