using System.Collections.ObjectModel;
using System.IO;
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

        public UserModulesWindow(int userId)
        {
            _user = Database.Users.FindById(userId);
            InitializeComponent();
            Title = $"Módulos en {_user.User}";
            ModulesGrid.ItemsSource = _modules;
            SearchText.TextChanged += (_, __) => LoadData();
            ReindexButton.Click += (_, __) => { ModuleUpdater.UpdateModules(_user.Id, _user.Path); LoadData(); };
            LoadData();
        }

        private void LoadData()
        {
            _modules.Clear();
            foreach (var module in Database.UserModules.FindByUserAndName(_user.Id, SearchText.Text))
            {
                var file = Path.Combine(_user.Path, module.AppName + ".exe");
                if (File.Exists(file))
                {
                    var currentModule = SiapReader.GetModuleModel(file);
                    module.AppName = currentModule.AppName;
                    module.AppVersion = currentModule.AppVersion;
                }
                _modules.Add(module);
            }
        }
    }
}

