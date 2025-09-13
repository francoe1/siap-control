using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;
using System.IO;
using System.Windows.Forms;

namespace SiapControl.Forms
{
    public partial class UserModulesForm : Form
    {
        private UserModel _user { get; set; }

        public UserModulesForm(int userId)
        {
            _user = Database.Users.FindById(userId);
            InitializeComponent();
            Text = $"Módulos en {_user.User}";
            LoadData();
        }

        private void LoadData()
        {
            dg1.Rows.Clear();

            foreach (ModuleModel module in Database.UserModules.FindByUserAndName(_user.Id, m_txt_search.Text))
            {
                string file = _user.Path + "\\" + module.AppName + ".exe";
                if (File.Exists(file))
                {
                    ModuleModel currentModule = SiapReader.GetModuleModel(file);
                    module.AppName = currentModule.AppName;
                    module.AppVersion = currentModule.AppVersion;
                }

                dg1.Rows.Add(module.AppName, module.AppVersion);
            }
        }

        private void m_txt_search_TextChanged(object sender, System.EventArgs e)
        {
            LoadData();
        }

        private void m_btn_reindex_click(object sender, System.EventArgs e)
        {
            ControlForm.UpdateModules(_user.Id, _user.Path);
            LoadData();
        }
    }
}