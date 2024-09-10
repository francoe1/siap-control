using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SiapControl.Forms
{
    public partial class ControlForm : Form
    {
        private int _currentRow { get; set; } = -1;

        public ControlForm()
        {
            InitializeComponent();
            Focus();
            dt1.UserDeletedRow += OnDeleteUser;
            dt1.CellDoubleClick += OnEditUser;
            dt1.CellClick += OnCellClick;
            Text += $" ({Assembly.GetExecutingAssembly().GetName().Version})";
            LoadDataAsync();


            ToolTip btnUpdateTooltip = new ToolTip();
            m_btn_update.Enabled = User.IsAdministrator;
            btnUpdateTooltip.SetToolTip(m_btn_update, "Require administrator right");
        }

        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            _currentRow = e.RowIndex;
            m_btn_modules.Enabled = _currentRow != -1;
        }

        private void OnEditUser(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;

            int id = (int)dt1.Rows[e.RowIndex].Cells[0].Value;
            UserModel user = Database.Users.FindById(id);
            UserForm form = new UserForm
            {
                UserName = user.User,
                SiapPath = user.Path
            };
            if (form.ShowDialog() == DialogResult.OK)
            {
                dt1.Rows[e.RowIndex].Cells[1].Value = form.UserName;
                dt1.Rows[e.RowIndex].Cells[2].Value = form.SiapPath;


                user.User = form.UserName;
                user.Path = form.SiapPath;
                Database.Users.Update(user);
            }
        }

        private void OnDeleteUser(object sender, DataGridViewRowEventArgs e)
        {
            int id = (int)e.Row.Cells[0].Value;
            Database.Users.Delete(id);
        }

        private async void LoadDataAsync()
        {
            string title = Text;
            Text += " (Cargando ...)";
            Enabled = false;
            await Database.ConnectAsync();
            UpdateUserTable();
            Text = title;
            Enabled = true;
        }

        private void UpdateUserTable()
        {
            dt1.Rows.Clear();
            foreach (UserModel info in Database.Users.FindAll())
            {
                dt1.Rows.Add(info.Id, info.User, info.Path);
                UpdateModules(info.Id, info.Path);
            }

            if (dt1.Rows.Count > 0)
            {
                _currentRow = 0;
                m_btn_modules.Enabled = true;
            }
        }

        public static void UpdateModules(int userId, string path)
        {
            if (!Directory.Exists(path)) return;

            try
            {
                SiapReader reader = new SiapReader(path);
                Database.UserModules.DeleteMany(x => x.UserId == userId);

                if (reader.Modules != null)
                {
                    foreach (ModuleModel module in reader.Modules)
                    {
                        module.UserId = userId;
                        Database.UserModules.Insert(module);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al intentar actualizar el modulo del usuario:{userId} \n{ex.Message}\n{ex.StackTrace}", "Error");
            }
        }

        private void m_btn_addUser_Click(object sender, System.EventArgs e)
        {
            UserForm form = new UserForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                Database.Users.Insert(new UserModel
                {
                    User = form.UserName,
                    Path = form.SiapPath,
                });
                UpdateUserTable();
            }
        }

        private void m_btn_update_Click(object sender, EventArgs e)
        {
            SetupReader setup = null;
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = @"Ejecutables|*.exe"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                setup = new SetupReader(dialog.FileName);
            }

            if (setup is null) return;
            if (!setup.Open()) return;

            InstallerForm form = new InstallerForm(setup);
            form.ShowDialog();
        }

        private void m_btn_modules_Click(object sender, EventArgs e)
        {
            if (_currentRow != -1)
            {
                UserModulesForm form = new UserModulesForm((int)dt1.Rows[_currentRow].Cells[0].Value);
                form.ShowDialog();
            }
        }
    }
}