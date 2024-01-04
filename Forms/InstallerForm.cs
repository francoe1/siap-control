using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

            toolStripStatusLabel1.Text = $"Actualizar {_setup.AppName} {_setup.AppVersion}";

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

            int count = 0;
            foreach (UserModel user in users)
            {
                count++;
                toolStripStatusLabel1.Text = $"{user.User} | Iniciando";
                File.WriteAllText(AFIP_PATH, user.Path);
                toolStripStatusLabel1.Text = $"{user.User} | Backup";
                _setup.CreateBackup(user.Path);
                toolStripStatusLabel1.Text = $"{user.User} | Instalando";
                if (await _setup.RunInstallerAsync(user.Path))
                {
                    string file = $@"{user.Path}\{_setup.AppName}\{_setup.AppName}.exe";
                    if (File.Exists(file))
                    {
                        FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);

                        Database.UpdateRegisters.Insert(new UpdateRegisterModel
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

            _setup.Close();
            m_btn_start.Enabled = true;
        }
    }
}