using SiapControl.Common;
using System;
using System.Windows.Forms;

namespace SiapControl
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!User.IsAdministrator)
                {
                    MessageBox.Show("Necesitas permisos de administrador", "Error");
                }

                Application.Run(new ControlForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message} ---> \n{ex.StackTrace}\n{ex.Source}\n{ex.TargetSite}", "Error");
            }
        }
    }
}