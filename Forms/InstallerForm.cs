using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SiapControl.Forms
{
    public partial class InstallerForm : Form
    {
        private const string AFIP_PATH = @"C:\Windows\afipPath.sys";
        private SetupReader _setup { get; set; }

        public InstallerForm(SetupReader setup)
        {
            _setup = setup;
            InitializeComponent();
            label1.Text = _setup.AppName;
            label2.Text = _setup.AppVersion;
            LoadDataGrid();
        }

        private void LoadDataGrid()
        {
            dg_1.Rows.Clear();
            foreach (UserModel info in Database.Users.FindAll())
            {
                string file = $@"{info.Path}\{_setup.AppName}\{_setup.AppName}.exe";
                FileVersionInfo version = null;
                if (File.Exists(file)) version = FileVersionInfo.GetVersionInfo(file);
                dg_1.Rows.Add(info.Id, info.User, version is null ? "Sin versión previa" : version.ProductVersion, true);
            }
        }

        private async void m_btn_start_ClickAsync(object sender, EventArgs e)
        {
            m_btn_start.Enabled = false;
            List<UserModel> users = new List<UserModel>();

            foreach (DataGridViewRow row in dg_1.Rows)
            {
                if (row.Cells["active"].Value is false) continue;

                int id = (int)row.Cells[0].Value;
                UserModel user = Database.Users.FindById(id);
                if (user is null) continue;
                users.Add(user);
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

            m_btn_start.Enabled = true;

            if (users.Count == installed)
            {
                Close();

                MessageBox.Show($"Se instalaron {installed} de {users.Count}", "Instalación Finalizada");
            }

        }
    }
}