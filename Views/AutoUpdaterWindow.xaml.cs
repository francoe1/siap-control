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

namespace SiapControl.Views
{
    public partial class AutoUpdaterWindow : Window
    {
        private readonly IAfipApplicationCatalogService _catalogService;
        private readonly IAfipInstallerPackageService _packageService;
        private readonly AutoUpdateRunner _runner;
        private readonly ObservableCollection<AutoUpdatePlanRow> _rows = new ObservableCollection<AutoUpdatePlanRow>();
        private List<AutoUpdatePlanRow> _allRows = new List<AutoUpdatePlanRow>();

        private sealed class AutoUpdatePlanRow : INotifyPropertyChanged
        {
            private bool _active;
            public event PropertyChangedEventHandler? PropertyChanged;

            public int UserId { get; set; }
            public int ModuleId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string ModuleName { get; set; } = string.Empty;
            public string InstalledVersion { get; set; } = string.Empty;
            public string RemoteName { get; set; } = string.Empty;
            public string RemoteVersion { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public bool CanUpdate { get; set; }
            public AutoUpdatePlanItem PlanItem { get; set; } = new AutoUpdatePlanItem();

            public bool Active
            {
                get => _active;
                set
                {
                    if (_active == value)
                    {
                        return;
                    }

                    _active = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active)));
                }
            }
        }

        public AutoUpdaterWindow()
            : this(new AfipApplicationCatalogService(), new AfipInstallerPackageService())
        {
        }

        public AutoUpdaterWindow(IAfipApplicationCatalogService catalogService, IAfipInstallerPackageService packageService)
        {
            _catalogService = catalogService;
            _packageService = packageService;
            _runner = new AutoUpdateRunner(_catalogService, _packageService, new AfipModuleMatcher());
            InitializeComponent();
            PlanGrid.ItemsSource = _rows;
            Loaded += async (_, __) => await CreatePlanAsync();
            SearchText.TextChanged += (_, __) => FilterRows();
            PlanButton.Click += async (_, __) => await CreatePlanAsync();
            StartButton.Click += async (_, __) => await StartAsync();
            StartButton.IsEnabled = false;
        }

        private void FilterRows()
        {
            string text = AfipModuleMatcher.Normalize(SearchText.Text);
            _rows.Clear();
            foreach (AutoUpdatePlanRow row in _allRows.Where(row => string.IsNullOrWhiteSpace(text) || AfipModuleMatcher.Normalize(row.UserName + " " + row.ModuleName + " " + row.RemoteName).Contains(text)))
            {
                _rows.Add(row);
            }

            StartButton.IsEnabled = _rows.Any(row => row.CanUpdate);
        }

        private async Task CreatePlanAsync()
        {
            try
            {
                SetBusy(true, "Evaluando modulos instalados contra ARCA...");
                _rows.Clear();
                IReadOnlyList<AutoUpdatePlanItem> plan = await _runner.BuildPlanAsync();
                _allRows = plan.Select(CreateRow).ToList();
                FilterRows();
                int updates = _allRows.Count(row => row.CanUpdate);
                StatusText.Text = $"Modulos evaluados: {_allRows.Count}. Desactualizados: {updates}.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo evaluar actualizaciones.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task StartAsync()
        {
            AutoUpdatePlanItem[] selected = _allRows
                .Where(row => row.Active && row.CanUpdate)
                .Select(row => row.PlanItem)
                .ToArray();

            if (selected.Length == 0)
            {
                MessageBox.Show(this, "Selecciona al menos un modulo desactualizado.", "Sin seleccion", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SetBusy(true, "Actualizando modulos seleccionados...");
                AutoUpdateRunResult result = await _runner.RunPlanAsync(selected);
                StatusText.Text = result.Summary;
                MessageBox.Show(this, result.Summary, "Actualizacion finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                await CreatePlanAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo ejecutar la actualizacion.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy, string? status = null)
        {
            SearchText.IsEnabled = !busy;
            PlanButton.IsEnabled = !busy;
            StartButton.IsEnabled = !busy && _rows.Any(row => row.CanUpdate);
            Progress.IsIndeterminate = busy;
            if (status != null)
            {
                StatusText.Text = status;
            }
        }

        private static AutoUpdatePlanRow CreateRow(AutoUpdatePlanItem item)
        {
            return new AutoUpdatePlanRow
            {
                UserId = item.User.Id,
                ModuleId = item.Module.Id,
                UserName = item.User.User,
                ModuleName = item.Module.AppName,
                InstalledVersion = item.Module.AppVersion,
                RemoteName = item.Package?.DisplayName ?? item.CatalogItem?.Title ?? string.Empty,
                RemoteVersion = item.Package?.VersionText ?? string.Empty,
                Status = GetStatusText(item),
                CanUpdate = item.CanUpdate,
                Active = item.CanUpdate,
                PlanItem = item
            };
        }

        private static string GetStatusText(AutoUpdatePlanItem item)
        {
            switch (item.Status)
            {
                case AutoUpdatePlanStatus.UpToDate:
                    return "Actualizado";
                case AutoUpdatePlanStatus.UpdateAvailable:
                    return "Actualizacion disponible";
                case AutoUpdatePlanStatus.NoSafeMatch:
                    return "Sin coincidencia segura";
                case AutoUpdatePlanStatus.NoCompatibleDownload:
                    return "Sin descarga compatible";
                case AutoUpdatePlanStatus.VersionNotComparable:
                    return "Version no comparable";
                case AutoUpdatePlanStatus.MissingUser:
                    return "Instalacion no encontrada";
                default:
                    return item.Message;
            }
        }
    }
}
