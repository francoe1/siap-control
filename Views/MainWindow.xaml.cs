using System.Collections.ObjectModel;
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

        public MainWindow()
        {
            InitializeComponent();

            UsersGrid.ItemsSource = _users;
            Loaded += OnLoaded;

            AddUserButton.Click += AddUser;
            UpdateButton.Click += UpdateApp;
            ModulesButton.Click += ShowModules;
            UsersGrid.MouseDoubleClick += EditUser;
            UsersGrid.SelectionChanged += OnSelectionChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await Database.ConnectAsync();
            LoadUsers();
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

        private void AddUser(object sender, RoutedEventArgs e)
        {
            var dialog = new UserWindow();
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

            var dialog = new UserWindow { UserName = user.User, SiapPath = user.Path };
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

            var win = new UserModulesWindow(user.Id);
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
                var win = new InstallerWindow(setup);
                win.ShowDialog();
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ModulesButton.IsEnabled = UsersGrid.SelectedItem != null;
        }
    }
}

