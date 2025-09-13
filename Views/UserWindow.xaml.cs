using System.IO;
using System.Windows;

namespace SiapControl.Views
{
    public partial class UserWindow : Window
    {
        public string UserName { get => UserText.Text; set => UserText.Text = value; }
        public string SiapPath { get => PathText.Text; set => PathText.Text = value; }

        public UserWindow()
        {
            InitializeComponent();
            SaveButton.Click += (_, __) => { DialogResult = true; Close(); };
            CancelButton.Click += (_, __) => { DialogResult = false; Close(); };
            BrowseButton.Click += OnBrowse;
        }

        private void OnBrowse(object? sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!File.Exists(System.IO.Path.Combine(dialog.SelectedPath, "siap.exe")))
                {
                    MessageBox.Show("La ruta seleccionada no es correcta, por favor seleccionar la ruta de instalación de SIAP", "Error");
                    return;
                }

                SiapPath = dialog.SelectedPath;
            }
        }
    }
}

