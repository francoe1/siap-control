using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SiapControl.Views
{
    public partial class InstallerWindow : Window
    {
        private const string AFIP_PATH = @"C:\Windows\afipPath.sys";
        private readonly SetupReader _setup;
        private readonly HashSet<int>? _targetUserIds;
        private readonly ObservableCollection<InstallUser> _rows = new();

        private class InstallUser : INotifyPropertyChanged
        {
            private string _version = "Pendiente";
            private string _status = "Pendiente";
            private bool _active = true;

            public event PropertyChangedEventHandler? PropertyChanged;

            public int Id { get; set; }
            public string User { get; set; } = string.Empty;

            public string Version
            {
                get => _version;
                set => SetField(ref _version, value);
            }

            public string Status
            {
                get => _status;
                set => SetField(ref _status, value);
            }

            public bool Active
            {
                get => _active;
                set => SetField(ref _active, value);
            }

            private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                {
                    return;
                }

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public InstallerWindow(SetupReader setup)
            : this(setup, null)
        {
        }

        public InstallerWindow(SetupReader setup, IEnumerable<int>? targetUserIds)
        {
            _setup = setup;
            _targetUserIds = targetUserIds == null ? null : new HashSet<int>(targetUserIds);
            InitializeComponent();
            TitleText.Text = _setup.AppName;
            VersionText.Text = $"Version a instalar: {_setup.AppVersion}";
            UsersGrid.ItemsSource = _rows;

            Loaded += OnLoadedAsync;
            StartButton.Click += StartAsync;
            StartMenuItem.Click += StartAsync;
            SelectAllButton.Click += (_, __) => SetAllRowsActive(true);
            SelectAllMenuItem.Click += (_, __) => SetAllRowsActive(true);
            SelectAllContextMenuItem.Click += (_, __) => SetAllRowsActive(true);
            ClearSelectionButton.Click += (_, __) => SetAllRowsActive(false);
            ClearSelectionMenuItem.Click += (_, __) => SetAllRowsActive(false);
            ClearSelectionContextMenuItem.Click += (_, __) => SetAllRowsActive(false);
            ToggleSelectedMenuItem.Click += ToggleSelectedRow;
            ToggleSelectedContextMenuItem.Click += ToggleSelectedRow;
            CloseMenuItem.Click += (_, __) => Close();
            UsersGrid.PreviewMouseRightButtonDown += SelectRowOnRightClick;
            UsersGrid.SelectionChanged += (_, __) => UpdateSelectionActions();
        }

        private async void OnLoadedAsync(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoadedAsync;
            await LoadDataGridAsync();
        }

        private async Task LoadDataGridAsync()
        {
            try
            {
                SetBusy(true);
                _rows.Clear();

                List<UserModel> users = Database.Users.FindAll()
                    .Where(user => _targetUserIds == null || _targetUserIds.Contains(user.Id))
                    .ToList();
                if (users.Count == 0)
                {
                    StatusText.Text = "No hay instalaciones cargadas.";
                    SetProgress(0, 0);
                    return;
                }

                SetProgress(0, users.Count);
                StatusText.Text = "Buscando versiones instaladas...";

                for (int i = 0; i < users.Count; i++)
                {
                    UserModel user = users[i];
                    var row = new InstallUser
                    {
                        Id = user.Id,
                        User = user.User,
                        Status = "Buscando version..."
                    };
                    _rows.Add(row);

                    row.Version = await Task.Run(() => GetInstalledVersion(user.Path));
                    row.Status = "Listo";
                    SetProgress(i + 1, users.Count);
                    StatusText.Text = $"Versiones detectadas: {i + 1} de {users.Count}";
                }

                StatusText.Text = "Selecciona las instalaciones que queres actualizar.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudieron leer las versiones instaladas.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private string GetInstalledVersion(string userPath)
        {
            string file = Path.Combine(userPath, _setup.AppName, _setup.AppName + ".exe");
            if (!File.Exists(file))
            {
                return "Sin version previa";
            }

            try
            {
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(file);
                return string.IsNullOrWhiteSpace(version.ProductVersion) ? "Sin version" : version.ProductVersion;
            }
            catch
            {
                return "No disponible";
            }
        }

        private async void StartAsync(object? sender, RoutedEventArgs e)
        {
            List<InstallUser> selectedRows = _rows.Where(r => r.Active).ToList();
            if (selectedRows.Count == 0)
            {
                MessageBox.Show(this, "Selecciona al menos una instalacion para actualizar.", "Sin seleccion", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<(InstallUser Row, UserModel User)> users = selectedRows
                .Select(row => (Row: row, User: Database.Users.FindById(row.Id)))
                .Where(item => item.User != null)
                .Select(item => (item.Row, item.User!))
                .ToList();

            if (users.Count == 0)
            {
                MessageBox.Show(this, "No se encontraron instalaciones validas para actualizar.", "Sin instalaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int installed = 0;
            try
            {
                SetBusy(true);
                SetProgress(0, users.Count);

                for (int i = 0; i < users.Count; i++)
                {
                    InstallUser row = users[i].Row;
                    UserModel user = users[i].User;
                    StatusText.Text = $"Actualizando {user.User} ({i + 1} de {users.Count})...";
                    row.Status = "Preparando...";

                    File.WriteAllText(AFIP_PATH, user.Path);

                    row.Status = "Creando backup...";
                    await _setup.CreateBackupAsync(user.Path);

                    SetupReader.Parameter titleParameter = _setup.Parameters.FirstOrDefault(x => x.Name.Equals("Title", StringComparison.OrdinalIgnoreCase));
                    string title = titleParameter.IsValid ? titleParameter.Value : _setup.AppName;
                    var setupAutoIntaller = new SetupAutoInstaller(title, _setup.Path);

                    row.Status = "Ejecutando instalador...";
                    if (await setupAutoIntaller.InstallAsync())
                    {
                        row.Status = "Actualizando modulo...";
                        await UpdateInstalledModuleAsync(user);
                        row.Version = await Task.Run(() => GetInstalledVersion(user.Path));
                        row.Status = "Actualizado";
                        installed++;
                    }
                    else
                    {
                        row.Status = "No actualizado";
                    }

                    SetProgress(i + 1, users.Count);
                }

                StatusText.Text = $"Actualizacion finalizada: {installed} de {users.Count}.";
                MessageBox.Show(this, $"Se actualizaron {installed} de {users.Count} instalaciones.", "Actualizacion finalizada", MessageBoxButton.OK, MessageBoxImage.Information);

                if (installed == users.Count)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo completar la actualizacion.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task UpdateInstalledModuleAsync(UserModel user)
        {
            string file = Path.Combine(user.Path, _setup.AppName, _setup.AppName + ".exe");
            if (!File.Exists(file))
            {
                return;
            }

            ModuleModel currentModule = await Task.Run(() => SiapReader.GetModuleModel(file));
            ModuleModel module = Database.UserModules.FindByUserAndAppName(user.Id, currentModule.AppName);

            if (module == null)
            {
                module = new ModuleModel
                {
                    UserId = user.Id,
                    AppVersion = currentModule.AppVersion,
                    AppName = currentModule.AppName,
                    LastUpdate = DateTime.Now
                };
                Database.UserModules.Insert(module);
            }
            else
            {
                module.AppVersion = currentModule.AppVersion;
                module.LastUpdate = DateTime.Now;
                Database.UserModules.Update(module);
            }
        }

        private void SetAllRowsActive(bool active)
        {
            foreach (InstallUser row in _rows)
            {
                row.Active = active;
            }
        }

        private void ToggleSelectedRow(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is InstallUser row)
            {
                row.Active = !row.Active;
            }
        }

        private void UpdateSelectionActions()
        {
            bool hasSelection = UsersGrid.IsEnabled && UsersGrid.SelectedItem != null;
            ToggleSelectedMenuItem.IsEnabled = hasSelection;
            ToggleSelectedContextMenuItem.IsEnabled = hasSelection;
        }

        private void SetBusy(bool isBusy)
        {
            UsersGrid.IsEnabled = !isBusy;
            StartButton.IsEnabled = !isBusy && _rows.Count > 0;
            StartMenuItem.IsEnabled = !isBusy && _rows.Count > 0;
            SelectAllButton.IsEnabled = !isBusy && _rows.Count > 0;
            SelectAllMenuItem.IsEnabled = !isBusy && _rows.Count > 0;
            SelectAllContextMenuItem.IsEnabled = !isBusy && _rows.Count > 0;
            ClearSelectionButton.IsEnabled = !isBusy && _rows.Count > 0;
            ClearSelectionMenuItem.IsEnabled = !isBusy && _rows.Count > 0;
            ClearSelectionContextMenuItem.IsEnabled = !isBusy && _rows.Count > 0;
            ToggleSelectedMenuItem.IsEnabled = !isBusy && UsersGrid.SelectedItem != null;
            ToggleSelectedContextMenuItem.IsEnabled = !isBusy && UsersGrid.SelectedItem != null;
            Progress.IsIndeterminate = isBusy && Progress.Value <= 0;
        }

        private void SetProgress(int current, int total)
        {
            Progress.IsIndeterminate = false;
            Progress.Value = total <= 0 ? 0 : Math.Round(current * 100d / total);
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
                row.IsSelected = true;
            }
        }
    }
}
