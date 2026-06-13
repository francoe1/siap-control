using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;

namespace SiapControl.Views
{
    public partial class InstallerWindow : Window
    {
        private const string AFIP_PATH = @"C:\\Windows\\afipPath.sys";
        private readonly SetupReader _setup;
        private readonly ObservableCollection<InstallUser> _rows = new();

        private class InstallUser
        {
            public int Id { get; set; }
            public string User { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public bool Active { get; set; } = true;
        }

        public InstallerWindow(SetupReader setup)
        {
            _setup = setup;
            InitializeComponent();
            TitleText.Text = _setup.AppName;
            VersionText.Text = _setup.AppVersion;
            UsersGrid.ItemsSource = _rows;
            LoadDataGrid();
            StartButton.Click += StartAsync;
        }

        private void LoadDataGrid()
        {
            _rows.Clear();
            foreach (UserModel info in Database.Users.FindAll())
            {
                string file = $@"{info.Path}\{_setup.AppName}\{_setup.AppName}.exe";
                FileVersionInfo? version = null;
                if (File.Exists(file)) version = FileVersionInfo.GetVersionInfo(file);
                _rows.Add(new InstallUser
                {
                    Id = info.Id,
                    User = info.User,
                    Version = version is null ? "Sin versión previa" : version.ProductVersion,
                    Active = true
                });
            }
        }

        private async void StartAsync(object? sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            List<UserModel> users = new();

            foreach (var row in _rows.Where(r => r.Active))
            {
                UserModel? user = Database.Users.FindById(row.Id);
                if (user != null)
                {
                    users.Add(user);
                }
            }

            int installed = 0;
            for (int i = 0; i < users.Count; i++)
            {
                UserModel user = users[i];
                File.WriteAllText(AFIP_PATH, user.Path);
                await _setup.CreateBackupAsync(user.Path);

                var title = _setup.Parameters.First(x => x.Name.Equals("Title")).Value;

                var setupAutoIntaller = new SetupAutoInstaller(title, _setup.Path);
                if (await setupAutoIntaller.InstallAsync())
                {
                    string file = $@"{user.Path}\{_setup.AppName}\{_setup.AppName}.exe";
                    if (File.Exists(file))
                    {
                        FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);

                        var module = Database.UserModules.FindByUserAndAppName(user.Id, info.ProductName);

                        if (module == null)
                        {
                            module = new ModuleModel
                            {
                                UserId = user.Id,
                                AppVersion = info.ProductVersion,
                                AppName = info.ProductName,
                                LastUpdate = DateTime.Now
                            };
                            Database.UserModules.Insert(module);
                        }
                        else
                        {
                            module.AppVersion = info.ProductVersion;
                            module.LastUpdate = DateTime.Now;
                            Database.UserModules.Update(module);
                        }
                    }
                    installed++;
                }
            }

            StartButton.IsEnabled = true;

            if (users.Count == installed)
            {
                Close();
                MessageBox.Show(this, $"Se instalaron {installed} de {users.Count}", "Instalación finalizada");
            }
        }
    }
}

