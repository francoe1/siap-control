using SiapControl.Common;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SiapControl
{
    public partial class ControlForm : Form
    {
        private int m_currentRow { get; set; } = -1;

        public ControlForm()
        {
            InitializeComponent();
            LoadData();
            dt1.UserDeletedRow += OnDeleteUser;
            dt1.CellDoubleClick += OnEditUser;
            dt1.CellClick += OnCellClick;
            Text += $" ({Assembly.GetExecutingAssembly().GetName().Version})";

            DbContext.ExportToJson();
        }

        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            m_currentRow = e.RowIndex;
            m_btn_modules.Enabled = m_currentRow != -1;
        }

        private void OnEditUser(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1) return;

            int id = (int)dt1.Rows[e.RowIndex].Cells[0].Value;
            UserModel user = DbContext.Users.FindById(id);
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
                DbContext.Users.Update(user);
                DbContext.ExportToJson();
            }
        }

        private void OnDeleteUser(object sender, DataGridViewRowEventArgs e)
        {
            int id = (int)e.Row.Cells[0].Value;
            DbContext.Users.Delete(id);
            DbContext.ExportToJson();
        }

        private void LoadData()
        {
            new DbContext();
            dt1.Rows.Clear();
            foreach (UserModel info in DbContext.Users.FindAll())
            {
                dt1.Rows.Add(info.Id, info.User, info.Path);
                UpdateModules(info.Id, info.Path);
            }

            if (dt1.Rows.Count > 0)
            {
                m_currentRow = 0;
                m_btn_modules.Enabled = true;
            }
        }

        public static void UpdateModules(int userId, string path)
        {
            if (!Directory.Exists(path)) return;

            try
            {
                SiapReader reader = new SiapReader(path);
                DbContext.UserModules.DeleteMany(x => x.UserId == userId);
                foreach (ModuleModel module in reader.Modules)
                {
                    module.UserId = userId;
                    DbContext.UserModules.Insert(module);
                }

                DbContext.ExportToJson();
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
                DbContext.Users.Insert(new UserModel
                {
                    User = form.UserName,
                    Path = form.SiapPath,
                });
                LoadData();
                DbContext.ExportToJson();
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

        private void m_btn_history_Click(object sender, EventArgs e)
        {
            HistoryForm form = new HistoryForm();
            form.ShowDialog();
        }

        private void m_btn_modules_Click(object sender, EventArgs e)
        {
            if (m_currentRow != -1)
            {
                UserModulesForm form = new UserModulesForm((int)dt1.Rows[m_currentRow].Cells[0].Value);
                form.ShowDialog();
            }
        }
    }
}