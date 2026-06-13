using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;

namespace SiapControl.Views
{
    public partial class UserModulesWindow : Window
    {
        private readonly UserModel _user;
        private readonly ObservableCollection<ModuleModel> _modules = new();
        private int _loadRequestId;

        public UserModulesWindow(int userId)
        {
            UserModel? user = Database.Users.FindById(userId);
            if (user == null)
            {
                throw new InvalidOperationException("No se encontro la instalacion seleccionada.");
            }

            _user = user;
            InitializeComponent();
            Title = $"Modulos en {_user.User}";
            ModulesGrid.ItemsSource = _modules;
            SearchText.TextChanged += async (_, __) => await LoadDataAsync();
            UpdateButton.Click += UpdateSelectedModuleAsync;
            UpdateMenuItem.Click += UpdateSelectedModuleAsync;
            UpdateContextMenuItem.Click += UpdateSelectedModuleAsync;
            ReindexButton.Click += ReindexAsync;
            ReindexMenuItem.Click += ReindexAsync;
            ReindexContextMenuItem.Click += ReindexAsync;
            CloseMenuItem.Click += (_, __) => Close();
            ModulesGrid.SelectionChanged += (_, __) => UpdateActions();
            Loaded += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            int requestId = ++_loadRequestId;
            List<ModuleModel> modules = Database.UserModules.FindByUserAndName(_user.Id, SearchText.Text).ToList();

            _modules.Clear();
            SetProgress(0, modules.Count);
            StatusText.Text = modules.Count == 0 ? "No hay modulos para mostrar." : "Leyendo versiones de modulos...";

            for (int i = 0; i < modules.Count; i++)
            {
                if (requestId != _loadRequestId)
                {
                    return;
                }

                ModuleModel module = modules[i];
                string executableName = string.IsNullOrWhiteSpace(module.ExecutableName) ? module.AppName : module.ExecutableName;
                string file = Path.Combine(_user.Path, executableName, executableName + ".exe");
                if (File.Exists(file))
                {
                    ModuleModel currentModule = await Task.Run(() => SiapReader.GetModuleModel(file));
                    module.AppName = currentModule.AppName;
                    module.AppVersion = currentModule.AppVersion;
                }

                _modules.Add(module);
                SetProgress(i + 1, modules.Count);
            }

            if (requestId == _loadRequestId)
            {
                StatusText.Text = $"Modulos visibles: {_modules.Count}.";
                UpdateActions();
            }
        }

        private async void UpdateSelectedModuleAsync(object sender, RoutedEventArgs e)
        {
            if (ModulesGrid.SelectedItem is not ModuleModel module)
            {
                MessageBox.Show(this, "Selecciona un modulo para actualizar.", "Sin seleccion", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SetBusy(true);
                StatusText.Text = "Evaluando actualizacion del modulo...";
                Progress.IsIndeterminate = true;

                var runner = new AutoUpdateRunner();
                IReadOnlyList<AutoUpdatePlanItem> plan = await runner.BuildPlanAsync(_user.Id, new[] { module.Id });
                AutoUpdatePlanItem? item = plan.FirstOrDefault();
                if (item == null)
                {
                    StatusText.Text = "No se pudo evaluar el modulo seleccionado.";
                    return;
                }

                if (!item.CanUpdate)
                {
                    StatusText.Text = GetPlanStatusText(item);
                    MessageBox.Show(this, GetPlanStatusText(item), "Sin actualizacion", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                StatusText.Text = $"Actualizando {module.AppName}...";
                AutoUpdateRunResult result = await runner.RunPlanAsync(new[] { item });
                StatusText.Text = result.Summary;
                MessageBox.Show(this, result.Summary, "Actualizacion finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo actualizar el modulo.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void ReindexAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                SetBusy(true);
                StatusText.Text = "Escaneando modulos de la instalacion...";
                Progress.IsIndeterminate = true;

                SiapReader reader = await Task.Run(() => new SiapReader(_user.Path));
                ModuleModel[] modules = reader.Modules ?? Array.Empty<ModuleModel>();

                StatusText.Text = "Guardando indice de modulos...";
                Progress.IsIndeterminate = false;
                SetProgress(0, modules.Length);

                Database.UserModules.DeleteByUser(_user.Id);
                for (int i = 0; i < modules.Length; i++)
                {
                    ModuleModel module = modules[i];
                    module.UserId = _user.Id;
                    Database.UserModules.Insert(module);
                    SetProgress(i + 1, modules.Length);
                }

                StatusText.Text = $"Reindexacion finalizada. Modulos encontrados: {modules.Length}.";
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"No se pudo reindexar la instalacion.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool isBusy)
        {
            SearchText.IsEnabled = !isBusy;
            UpdateButton.IsEnabled = !isBusy && ModulesGrid.SelectedItem != null;
            UpdateMenuItem.IsEnabled = !isBusy && ModulesGrid.SelectedItem != null;
            UpdateContextMenuItem.IsEnabled = !isBusy && ModulesGrid.SelectedItem != null;
            ReindexButton.IsEnabled = !isBusy;
            ReindexMenuItem.IsEnabled = !isBusy;
            ReindexContextMenuItem.IsEnabled = !isBusy;
            ModulesGrid.IsEnabled = !isBusy;
        }

        private void UpdateActions()
        {
            bool hasSelection = ModulesGrid.SelectedItem != null;
            UpdateButton.IsEnabled = hasSelection;
            UpdateMenuItem.IsEnabled = hasSelection;
            UpdateContextMenuItem.IsEnabled = hasSelection;
        }

        private void SetProgress(int current, int total)
        {
            Progress.IsIndeterminate = false;
            Progress.Value = total <= 0 ? 0 : Math.Round(current * 100d / total);
        }

        private static string GetPlanStatusText(AutoUpdatePlanItem item)
        {
            switch (item.Status)
            {
                case AutoUpdatePlanStatus.UpToDate:
                    return "El modulo ya esta actualizado.";
                case AutoUpdatePlanStatus.NoSafeMatch:
                    return "No se encontro una coincidencia segura en ARCA.";
                case AutoUpdatePlanStatus.NoCompatibleDownload:
                    return "No se encontro una descarga compatible en ARCA.";
                case AutoUpdatePlanStatus.VersionNotComparable:
                    return item.Message;
                default:
                    return item.Message;
            }
        }
    }
}
