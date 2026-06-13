using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;

namespace SiapControl.Views
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<InstallationRow> _users = new();
        private bool _databaseReady;

        private sealed class InstallationRow : INotifyPropertyChanged
        {
            private string _validationStatus = "Sin validar";

            public event PropertyChangedEventHandler? PropertyChanged;

            public int Id { get; set; }
            public string User { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;

            public string ValidationStatus
            {
                get => _validationStatus;
                set
                {
                    if (_validationStatus == value)
                    {
                        return;
                    }

                    _validationStatus = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationStatus)));
                }
            }

            public UserModel ToUserModel()
            {
                return new UserModel
                {
                    Id = Id,
                    User = User,
                    Path = Path
                };
            }
        }

        private readonly struct InstallationValidationResult
        {
            public InstallationValidationResult(bool isValid, string message)
            {
                IsValid = isValid;
                Message = message;
            }

            public bool IsValid { get; }
            public string Message { get; }
        }

        public MainWindow()
        {
            InitializeComponent();
            Title = $"SiapControl v{GetApplicationVersion()}";

            UsersGrid.ItemsSource = _users;
            Loaded += OnLoaded;

            CheckUpdateMenuItem.Click += CheckForUpdatesManually;
            CheckUpdateContextMenuItem.Click += CheckForUpdatesManually;
            SearchInstallationsMenuItem.Click += SearchInstallations;
            SearchInstallationsContextMenuItem.Click += SearchInstallations;
            AddUserMenuItem.Click += AddUser;
            AddUserContextMenuItem.Click += AddUser;
            DeleteUserMenuItem.Click += DeleteUserAsync;
            DeleteUserContextMenuItem.Click += DeleteUserAsync;
            UpdateAppMenuItem.Click += UpdateApp;
            UpdateAppContextMenuItem.Click += UpdateApp;
            ModulesMenuItem.Click += ShowModules;
            ModulesContextMenuItem.Click += ShowModules;
            ValidateAllMenuItem.Click += ValidateAllInstallationsAsync;
            ValidateAllContextMenuItem.Click += ValidateAllInstallationsAsync;
            ValidateSelectedMenuItem.Click += ValidateSelectedInstallationsAsync;
            ValidateSelectedContextMenuItem.Click += ValidateSelectedInstallationsAsync;
            EditUserMenuItem.Click += EditUser;
            EditUserContextMenuItem.Click += EditUser;
            ExitMenuItem.Click += (_, __) => Close();
            AboutMenuItem.Click += ShowAbout;
            UsersGrid.PreviewMouseRightButtonDown += SelectRowOnRightClick;
            UsersGrid.MouseDoubleClick += EditUserFromMouse;
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
                SetStatus("Base de datos lista.", 0);
                SetControlsEnabled(true);
                LoadUsers();
                _ = CheckForUpdatesAsync(showNoUpdateMessage: false);
            }
            catch (Exception ex)
            {
                SetStatus("No se pudo abrir la base de datos.", 0);
                MessageBox.Show($"No se pudo abrir la base de datos.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadUsers()
        {
            _users.Clear();
            foreach (UserModel user in Database.Users.FindAll())
            {
                _users.Add(new InstallationRow
                {
                    Id = user.Id,
                    User = user.User,
                    Path = user.Path
                });
            }

            StatusCountText.Text = _users.Count == 1 ? "1 instalacion" : $"{_users.Count} instalaciones";
            UpdateSelectionActions();
        }

        private void ShowStartupProgress(DatabaseInitializationProgress progress)
        {
            SetStatus(progress.Message, progress.Percentage);
        }

        private async void CheckForUpdatesManually(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync(showNoUpdateMessage: true);
        }

        private async Task CheckForUpdatesAsync(bool showNoUpdateMessage)
        {
            try
            {
                CheckUpdateMenuItem.IsEnabled = false;
                CheckUpdateContextMenuItem.IsEnabled = false;
                SetStatus("Buscando actualizaciones de SiapControl...", 0);
                StartupProgressBar.IsIndeterminate = true;

                var updateService = new GitHubUpdateService();
                GitHubUpdate? update = await updateService.CheckForUpdateAsync();
                if (update == null)
                {
                    SetStatus("SiapControl esta actualizado.", 0);
                    if (showNoUpdateMessage)
                    {
                        MessageBox.Show(this, "Ya tenes la ultima version disponible.", "Sin actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    return;
                }

                StartupProgressBar.IsIndeterminate = false;
                SetStatus($"Actualizacion disponible: {update.Version}.", 100);

                MessageBoxResult result = MessageBox.Show(
                    this,
                    $"Hay una nueva version disponible: {update.Version}.\nQueres descargarla e instalarla ahora?",
                    "Actualizacion disponible",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                {
                    SetStatus("Actualizacion cancelada.", 0);
                    return;
                }

                SetControlsEnabled(false);
                StartupStatusText.Text = "Descargando actualizacion...";
                StartupProgressBar.IsIndeterminate = true;

                await updateService.DownloadAndInstallAsync(update);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                SetStatus("No se pudo buscar actualizaciones.", 0);
                if (showNoUpdateMessage)
                {
                    MessageBox.Show(this, $"No se pudo buscar actualizaciones.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                if (_databaseReady)
                {
                    StartupProgressBar.IsIndeterminate = false;
                    SetControlsEnabled(true);
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
            CheckUpdateMenuItem.IsEnabled = isEnabled;
            CheckUpdateContextMenuItem.IsEnabled = isEnabled;
            SearchInstallationsMenuItem.IsEnabled = isEnabled;
            SearchInstallationsContextMenuItem.IsEnabled = isEnabled;
            AddUserMenuItem.IsEnabled = isEnabled;
            AddUserContextMenuItem.IsEnabled = isEnabled;
            UpdateAppMenuItem.IsEnabled = isEnabled;
            UpdateAppContextMenuItem.IsEnabled = isEnabled;
            ValidateAllMenuItem.IsEnabled = isEnabled;
            ValidateAllContextMenuItem.IsEnabled = isEnabled;
            UpdateSelectionActions(isEnabled);
        }

        private async void SearchInstallations(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Selecciona la carpeta donde buscar instalaciones de SIAP"
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            try
            {
                SetControlsEnabled(false);
                StartupStatusText.Text = "Buscando instalaciones de SIAP...";
                StartupProgressBar.IsIndeterminate = true;

                IReadOnlyList<SiapInstallation> installations = await Task.Run(
                    () => SiapInstallationFinder.FindAll(dialog.SelectedPath));

                var existingPaths = new HashSet<string>(
                    Database.Users.FindAll().Select(user => NormalizePath(user.Path)),
                    StringComparer.OrdinalIgnoreCase);

                int added = 0;
                int skipped = 0;

                foreach (SiapInstallation installation in installations)
                {
                    string normalizedPath = NormalizePath(installation.Path);
                    if (!existingPaths.Add(normalizedPath))
                    {
                        skipped++;
                        continue;
                    }

                    var model = new UserModel
                    {
                        User = installation.Name,
                        Path = installation.Path
                    };

                    Database.Users.Insert(model);
                    added++;
                }

                LoadUsers();
                SetStatus($"Busqueda finalizada. Agregadas: {added}. Existentes: {skipped}.", 100);

                MessageBox.Show(
                    this,
                    $"Instalaciones encontradas: {installations.Count}\nAgregadas: {added}\nYa existentes: {skipped}",
                    "Busqueda finalizada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SetStatus("No se pudo buscar instalaciones.", 0);
                MessageBox.Show(this, $"No se pudo buscar instalaciones de SIAP.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                StartupProgressBar.IsIndeterminate = false;
                SetControlsEnabled(true);
            }
        }

        private static string NormalizePath(string path)
        {
            string normalized = path;
            try
            {
                normalized = Path.GetFullPath(path);
            }
            catch (Exception)
            {
            }

            return normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private void AddUser(object sender, RoutedEventArgs e)
        {
            var dialog = new UserWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var model = new UserModel { User = dialog.UserName, Path = dialog.SiapPath };
                Database.Users.Insert(model);
                LoadUsers();
                SetStatus("Instalacion agregada.", 0);
            }
        }

        private async void DeleteUserAsync(object sender, RoutedEventArgs e)
        {
            List<InstallationRow> selectedRows = GetSelectedRows();
            if (selectedRows.Count == 0)
            {
                return;
            }

            string subject = selectedRows.Count == 1
                ? $"la instalacion '{selectedRows[0].User}'"
                : $"{selectedRows.Count} instalaciones";

            MessageBoxResult result = MessageBox.Show(
                this,
                $"Se va a eliminar {subject}.\nNo se borran archivos del disco, solo registros en SiapControl.",
                "Eliminar instalaciones",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                SetControlsEnabled(false);
                StartupStatusText.Text = selectedRows.Count == 1 ? "Eliminando instalacion..." : "Eliminando instalaciones...";
                StartupProgressBar.IsIndeterminate = true;

                await Task.Run(() =>
                {
                    foreach (InstallationRow row in selectedRows)
                    {
                        Database.UserModules.DeleteByUser(row.Id);
                        Database.Users.Delete(row.Id);
                    }
                });

                LoadUsers();
                SetStatus(selectedRows.Count == 1 ? "Instalacion eliminada." : "Instalaciones eliminadas.", 100);
            }
            catch (Exception ex)
            {
                SetStatus("No se pudieron eliminar las instalaciones.", 0);
                MessageBox.Show(this, $"No se pudieron eliminar las instalaciones.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                StartupProgressBar.IsIndeterminate = false;
                SetControlsEnabled(true);
            }
        }

        private void EditUser(object sender, RoutedEventArgs e)
        {
            EditSelectedUser();
        }

        private void EditUserFromMouse(object sender, MouseButtonEventArgs e)
        {
            EditSelectedUser();
        }

        private void EditSelectedUser()
        {
            if (UsersGrid.SelectedItem is not InstallationRow row)
            {
                return;
            }

            UserModel user = row.ToUserModel();
            var dialog = new UserWindow { Owner = this, UserName = user.User, SiapPath = user.Path };
            if (dialog.ShowDialog() == true)
            {
                user.User = dialog.UserName;
                user.Path = dialog.SiapPath;
                Database.Users.Update(user);
                LoadUsers();
                SetStatus("Instalacion actualizada.", 0);
            }
        }

        private void ShowModules(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not InstallationRow user)
            {
                return;
            }

            var win = new UserModulesWindow(user.Id) { Owner = this };
            win.ShowDialog();
        }

        private async void ValidateAllInstallationsAsync(object sender, RoutedEventArgs e)
        {
            await ValidateInstallationsAsync(_users.ToList());
        }

        private async void ValidateSelectedInstallationsAsync(object sender, RoutedEventArgs e)
        {
            await ValidateInstallationsAsync(GetSelectedRows());
        }

        private async Task ValidateInstallationsAsync(List<InstallationRow> rows)
        {
            if (rows.Count == 0)
            {
                MessageBox.Show(this, "Selecciona al menos una instalacion para validar.", "Sin seleccion", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SetControlsEnabled(false);
                SetStatus("Validando instalaciones...", 0);

                int valid = 0;
                int invalid = 0;

                for (int i = 0; i < rows.Count; i++)
                {
                    InstallationRow row = rows[i];
                    row.ValidationStatus = "Validando...";

                    InstallationValidationResult result = await Task.Run(() => ValidateInstallation(row.Path));
                    row.ValidationStatus = result.Message;

                    if (result.IsValid)
                    {
                        valid++;
                    }
                    else
                    {
                        invalid++;
                    }

                    SetStatus($"Validando instalaciones {i + 1} de {rows.Count}...", Math.Round((i + 1) * 100d / rows.Count));
                }

                SetStatus($"Validacion finalizada. Correctas: {valid}. Con problemas: {invalid}.", 100);
            }
            catch (Exception ex)
            {
                SetStatus("No se pudo validar instalaciones.", 0);
                MessageBox.Show(this, $"No se pudo validar instalaciones.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                StartupProgressBar.IsIndeterminate = false;
                SetControlsEnabled(true);
            }
        }

        private static InstallationValidationResult ValidateInstallation(string path)
        {
            if (!Directory.Exists(path))
            {
                return new InstallationValidationResult(false, "No existe la carpeta");
            }

            bool hasSiap = File.Exists(System.IO.Path.Combine(path, "siap.exe"));
            bool hasAfip = File.Exists(System.IO.Path.Combine(path, "AFIP.MDB"));

            if (hasSiap && hasAfip)
            {
                return new InstallationValidationResult(true, "Correcta");
            }

            if (!hasSiap && !hasAfip)
            {
                return new InstallationValidationResult(false, "Falta siap.exe y AFIP.MDB");
            }

            return new InstallationValidationResult(false, hasSiap ? "Falta AFIP.MDB" : "Falta siap.exe");
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
                SetStatus("Actualizacion de modulo finalizada.", 0);
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionActions();
        }

        private void SelectRowOnRightClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            while (source != null && source is not DataGridRow)
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if (source is DataGridRow row)
            {
                if (!row.IsSelected)
                {
                    UsersGrid.SelectedItems.Clear();
                    row.IsSelected = true;
                }
            }
        }

        private List<InstallationRow> GetSelectedRows()
        {
            return UsersGrid.SelectedItems
                .OfType<InstallationRow>()
                .ToList();
        }

        private void UpdateSelectionActions(bool controlsEnabled = true)
        {
            int selectedCount = UsersGrid.SelectedItems.Count;
            bool hasSelection = _databaseReady && controlsEnabled && selectedCount > 0;
            bool hasSingleSelection = _databaseReady && controlsEnabled && selectedCount == 1;

            DeleteUserMenuItem.IsEnabled = hasSelection;
            DeleteUserContextMenuItem.IsEnabled = hasSelection;
            ValidateSelectedMenuItem.IsEnabled = hasSelection;
            ValidateSelectedContextMenuItem.IsEnabled = hasSelection;
            EditUserMenuItem.IsEnabled = hasSingleSelection;
            EditUserContextMenuItem.IsEnabled = hasSingleSelection;
            ModulesMenuItem.IsEnabled = hasSingleSelection;
            ModulesContextMenuItem.IsEnabled = hasSingleSelection;
        }

        private void SetStatus(string message, double progress)
        {
            StartupStatusText.Text = message;
            StartupProgressBar.IsIndeterminate = false;
            StartupProgressBar.Value = progress;
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                this,
                $"SiapControl v{GetApplicationVersion()}\nAdministrador de multiples instalaciones de SIAP.\nAutor: Franco Rosatto",
                "Acerca de SiapControl",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
