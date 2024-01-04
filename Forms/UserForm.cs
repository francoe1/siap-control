using System;
using System.IO;
using System.Windows.Forms;

namespace SiapControl
{
    public partial class UserForm : Form
    {
        public string UserName
        { get { return m_txt_user.Text; } set { m_txt_user.Text = value; } }

        public string SiapPath
        { get { return m_text_path.Text; } set { m_text_path.Text = value; } }

        public UserForm()
        {
            InitializeComponent();
            m_btn_save.Click += OnSave;
            m_btn_cancel.Click += OnCancel;
            m_btn_findPath.Click += OnFindPath;
        }

        private void OnFindPath(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (!File.Exists(dialog.SelectedPath + "\\siap.exe"))
                {
                    MessageBox.Show("La ruta seleccionada no es correcta, por favor seleccionar la ruta de instalación de SIAP", "Error");
                    return;
                }

                SiapPath = dialog.SelectedPath;
            }
        }

        private void OnCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnSave(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}