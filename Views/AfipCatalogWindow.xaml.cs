using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SiapControl.Views
{
    public partial class AfipCatalogWindow : Window
    {
        private readonly IAfipApplicationCatalogService _catalogService;
        private readonly IAfipInstallerPackageService _packageService;
        private readonly ObservableCollection<CatalogRow> _rows = new ObservableCollection<CatalogRow>();
        private List<CatalogRow> _allRows = new List<CatalogRow>();
        private bool _busy;

        private sealed class CatalogRow : INotifyPropertyChanged
        {
            private string _version = string.Empty;
            private DateTime? _releaseDate;
            private string _status = "Pendiente";
            private int _progress;
            private AfipApplicationPackage? _package;

            public event PropertyChangedEventHandler? PropertyChanged;

            public AfipApplicationCatalogItem Item { get; set; } = new AfipApplicationCatalogItem();
            public string Name => Package?.DisplayName ?? Item.Title;
            public string Category => Item.Category;

            public string Version
            {
                get => _version;
                set => SetField(ref _version, value);
            }

            public DateTime? ReleaseDate
            {
                get => _releaseDate;
                set
                {
                    if (_releaseDate == value)
                    {
                        return;
                    }

                    _releaseDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ReleaseDateText));
                }
            }

            public string ReleaseDateText => ReleaseDate.HasValue ? ReleaseDate.Value.ToString("dd/MM/yyyy") : string.Empty;
            public DateTime ReleaseDateSortKey => ReleaseDate ?? DateTime.MinValue;
            public string VersionSortKey => Version;

            public string Status
            {
                get => _status;
                set => SetField(ref _status, value);
            }

            public int Progress
            {
                get => _progress;
                set
                {
                    if (_progress == value)
                    {
                        return;
                    }

                    _progress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressText));
                }
            }

            public string ProgressText => Progress <= 0 ? string.Empty : Progress + "%";

            public AfipApplicationPackage? Package
            {
                get => _package;
                set
                {
                    if (_package == value)
                    {
                        return;
                    }

                    _package = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(CanInstall));
                }
            }

            public bool CanInstall => Package != null;

            private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                {
                    return;
                }

                field = value;
                OnPropertyChanged(propertyName);
            }

            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public AfipCatalogWindow()
            : this(new AfipApplicationCatalogService(), new AfipInstallerPackageService())
        {
        }

        public AfipCatalogWindow(IAfipApplicationCatalogService catalogService, IAfipInstallerPackageService packageService)
        {
            _catalogService = catalogService;
            _packageService = packageService;
            InitializeComponent();
            CatalogGrid.ItemsSource = _rows;
            Loaded += async (_, __) => await LoadCatalogAsync();
            SearchText.TextChanged += (_, __) => ApplyFilterAndSort();
            SortCombo.SelectionChanged += (_, __) => ApplyFilterAndSort();
            CatalogGrid.SelectionChanged += (_, __) => UpdateActions();
            RefreshButton.Click += async (_, __) => await LoadCatalogAsync();
            InstallButton.Click += async (_, __) => await InstallSelectedAsync();
            InstallButton.IsEnabled = false;
        }

        private async Task LoadCatalogAsync()
        {
            try
            {
                SetBusy(true, "Cargando catalogo de ARCA...");
                IReadOnlyList<AfipApplicationCatalogItem> catalog = await _catalogService.GetCatalogAsync();
                List<CatalogRow> resolvedRows = catalog.Select(item => new CatalogRow { Item = item }).ToList();
                _allRows = new List<CatalogRow>();
                _rows.Clear();

                int resolved = 0;
                for (int i = 0; i < resolvedRows.Count; i++)
                {
                    CatalogRow row = resolvedRows[i];
                    row.Status = "Leyendo detalle...";
                    AfipApplicationPackage? package = await _catalogService.ResolvePackageAsync(row.Item);
                    row.Package = package;
                    row.Version = package?.VersionText ?? string.Empty;
                    row.ReleaseDate = package?.PublishedAt;
                    row.Status = package == null ? "Sin instalador compatible" : "Disponible";
                    if (package != null)
                    {
                        resolved++;
                    }

                    SetProgress(i + 1, resolvedRows.Count);
                    StatusText.Text = $"Catalogo ARCA: {i + 1} de {resolvedRows.Count}. Instalables: {resolved}.";
                }

                _allRows = KeepLatestRows(resolvedRows);
                ApplyFilterAndSort();
                StatusText.Text = $"Catalogo cargado. Modulos: {_allRows.Count}. Instalables: {_allRows.Count(row => row.CanInstall)}.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo cargar el catalogo de ARCA.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task InstallSelectedAsync()
        {
            CatalogRow[] selectedRows = CatalogGrid.SelectedItems
                .OfType<CatalogRow>()
                .Where(row => row.Package != null)
                .ToArray();

            if (selectedRows.Length == 0)
            {
                MessageBox.Show(this, "Selecciona al menos un aplicativo instalable.", "Sin seleccion", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int[] userIds = Database.Users.FindAll().Select(user => user.Id).ToArray();
            if (userIds.Length == 0)
            {
                MessageBox.Show(this, "No hay instalaciones SIAP cargadas.", "Sin instalaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SetBusy(true, "Preparando actualizaciones...");
                int totalSteps = selectedRows.Length * 3;
                int completedSteps = 0;
                int installed = 0;

                for (int i = 0; i < selectedRows.Length; i++)
                {
                    CatalogRow row = selectedRows[i];
                    StatusText.Text = $"Descargando {row.Name} ({i + 1} de {selectedRows.Length})...";
                    row.Status = "Descargando...";
                    row.Progress = 0;
                    SetProgress(completedSteps, totalSteps);

                    var downloadProgress = new Progress<AfipPackageProgress>(packageProgress =>
                    {
                        row.Status = packageProgress.Message;
                        row.Progress = packageProgress.Percentage;
                        StatusText.Text = packageProgress.Message;
                    });

                    SetupReader? setup = await _packageService.DownloadAndPrepareAsync(row.Package!, downloadProgress);
                    completedSteps++;
                    SetProgress(completedSteps, totalSteps);

                    if (setup == null)
                    {
                        row.Status = "No se pudo preparar";
                        row.Progress = 0;
                        continue;
                    }

                    StatusText.Text = $"Instalando {row.Name} en {userIds.Length} SIAP...";
                    row.Status = $"Instalando en {userIds.Length} SIAP...";
                    row.Progress = 70;
                    var installer = new InstallerWindow(setup, userIds) { Owner = this };
                    completedSteps++;
                    SetProgress(completedSteps, totalSteps);

                    installer.ShowDialog();
                    completedSteps++;
                    installed++;
                    row.Status = "Instalado";
                    row.Progress = 100;
                    SetProgress(completedSteps, totalSteps);
                }

                StatusText.Text = $"Instalacion finalizada. Aplicativos procesados: {installed} de {selectedRows.Length}.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo instalar el aplicativo.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void ApplyFilterAndSort()
        {
            string search = AfipModuleMatcher.Normalize(SearchText.Text);
            IEnumerable<CatalogRow> rows = _allRows.Where(row =>
                string.IsNullOrWhiteSpace(search) ||
                AfipModuleMatcher.Normalize(row.Name + " " + row.Category + " " + row.Version).Contains(search));

            if ((SortCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Nombre")
            {
                rows = rows.OrderBy(row => row.Name);
            }
            else
            {
                rows = rows.OrderByDescending(row => row.ReleaseDateSortKey).ThenBy(row => row.Name);
            }

            _rows.Clear();
            foreach (CatalogRow row in rows)
            {
                _rows.Add(row);
            }

            UpdateActions();
        }

        private void UpdateActions()
        {
            InstallButton.IsEnabled = !_busy && CatalogGrid.SelectedItems.OfType<CatalogRow>().Any(row => row.CanInstall);
        }

        private void SetBusy(bool busy, string? status = null)
        {
            _busy = busy;
            SearchText.IsEnabled = !busy;
            SortCombo.IsEnabled = !busy;
            RefreshButton.IsEnabled = !busy;
            CatalogGrid.IsEnabled = !busy;
            Progress.IsIndeterminate = busy;
            if (status != null)
            {
                StatusText.Text = status;
            }

            UpdateActions();
        }

        private void SetProgress(int current, int total)
        {
            Progress.IsIndeterminate = false;
            Progress.Value = total <= 0 ? 0 : Math.Round(current * 100d / total);
        }

        private static List<CatalogRow> KeepLatestRows(IEnumerable<CatalogRow> rows)
        {
            return rows
                .GroupBy(GetModuleKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(row => row, CatalogRowLatestComparer.Instance).First())
                .ToList();
        }

        private static string GetModuleKey(CatalogRow row)
        {
            string name = row.Package?.DisplayName ?? row.Item.Title;
            string normalized = AfipModuleMatcher.Normalize(AfipVersionComparer.RemoveVersionText(name));
            return string.IsNullOrWhiteSpace(normalized) ? AfipModuleMatcher.Normalize(name) : normalized;
        }

        private sealed class CatalogRowLatestComparer : IComparer<CatalogRow>
        {
            public static readonly CatalogRowLatestComparer Instance = new CatalogRowLatestComparer();

            public int Compare(CatalogRow? x, CatalogRow? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                if (AfipVersionComparer.TryCompare(x.Version, y.Version, out int versionComparison) && versionComparison != 0)
                {
                    return versionComparison;
                }

                int dateComparison = x.ReleaseDateSortKey.CompareTo(y.ReleaseDateSortKey);
                if (dateComparison != 0)
                {
                    return dateComparison;
                }

                return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
