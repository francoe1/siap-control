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
            ReindexButton.Click += ReindexAsync;
            ReindexMenuItem.Click += ReindexAsync;
            ReindexContextMenuItem.Click += ReindexAsync;
            CloseMenuItem.Click += (_, __) => Close();
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
                string file = Path.Combine(_user.Path, module.AppName + ".exe");
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
            ReindexButton.IsEnabled = !isBusy;
            ReindexMenuItem.IsEnabled = !isBusy;
            ReindexContextMenuItem.IsEnabled = !isBusy;
            ModulesGrid.IsEnabled = !isBusy;
        }

        private void SetProgress(int current, int total)
        {
            Progress.IsIndeterminate = false;
            Progress.Value = total <= 0 ? 0 : Math.Round(current * 100d / total);
        }
    }
}
