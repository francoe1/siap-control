using System;
using System.Linq;
using System.Windows.Forms;

namespace SiapControl
{
    public partial class HistoryForm : Form
    {
        public HistoryForm()
        {
            InitializeComponent();

            LoadData();
        }

        private void LoadData()
        {
            dg1.Rows.Clear();

            foreach (UpdateRegister register in DbContext.UpdateRegisters
                .Find(x => x.Date.AddDays(90) > DateTime.Now && (
                x.AppName.ToLower().Contains(m_txt_search.Text) ||
                x.AppVersion.ToLower().Contains(m_txt_search.Text))).OrderByDescending(x => x.Date))
            {
                UserModel user = DbContext.Users.FindById(register.UserId);
                if (user is null) continue;

                dg1.Rows.Add(user.User, register.AppName, register.AppVersion, register.Date);
            }
        }

        private void m_txt_search_TextChanged(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}