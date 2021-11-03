using SiapControl.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SiapControl
{
    public partial class InstallerForm : Form
    {
        private const string AFIP_PATH = @"C:\Windows\afipPath.sys";
        private SetupReader m_setup { get; set; }

        public InstallerForm(SetupReader setup)
        {
            m_setup = setup;
            InitializeComponent();

            toolStripStatusLabel1.Text = $"Actualizar {m_setup.AppName} {m_setup.AppVersion}";

            label1.Text = m_setup.AppName;
            label2.Text = m_setup.AppVersion;

            LoadDataGrid();
        }

        private void LoadDataGrid()
        {
            dg_1.Rows.Clear();
            foreach (UserModel info in DbContext.Users.FindAll())
            {
                string file = $@"{info.Path}\{m_setup.AppName}\{m_setup.AppName}.exe";
                FileVersionInfo version = null;
                if (File.Exists(file))  version = FileVersionInfo.GetVersionInfo(file);
               dg_1.Rows.Add(info.Id, info.User, version is null ? "Sin versión previa" :  version.ProductVersion, true);
            }
        }

        private async void m_btn_start_Click(object sender, EventArgs e)
        {
            m_btn_start.Enabled = false;
            List<UserModel> users = new List<UserModel>();

            foreach (DataGridViewRow row in dg_1.Rows)
            {
                if (row.Cells["active"].Value is false) continue;

                int id = (int)row.Cells[0].Value;
                UserModel user = DbContext.Users.FindById(id);
                if (user is null) continue;
                users.Add(user);
            }


            int count = 0;
            foreach (UserModel user in users)
            {
                count++;
                toolStripStatusLabel1.Text = $"{user.User} | Iniciando";
                File.WriteAllText(AFIP_PATH, user.Path);
                toolStripStatusLabel1.Text = $"{user.User} | Backup";
                m_setup.CreateBackup(user.Path);
                toolStripStatusLabel1.Text = $"{user.User} | Instalando";
                if (await m_setup.RunInstallerAsync())
                {
                    string file = $@"{user.Path}\{m_setup.AppName}\{m_setup.AppName}.exe";
                    if (File.Exists(file))
                    {
                        FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);

                        DbContext.UpdateRegisters.Insert(new UpdateRegister
                        {
                            AppName = info.ProductName,
                            AppVersion = info.ProductVersion,
                            UserId = user.Id,
                            Date = DateTime.Now,
                        });
                    }                    
                }

                toolStripProgressBar1.Value = (int)(count / (float)users.Count * 100);
                ControlForm.UpdateModules(user.Id, user.Path);
            }
            m_btn_start.Enabled = true;
        }
    }
}
