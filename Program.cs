using System;
using System.Security.Principal;
using System.Windows.Forms;

namespace SiapControl
{
    internal static class Program
    {
        public static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (!IsElevated)
                {
                    MessageBox.Show("Necesitas permisos de administrador", "Error");
                }

                NetVersion netVersion = new NetVersion();

                if (netVersion.VersionN < 460)
                {
                    MessageBox.Show($"Requires de una versión 4.6.0 o superior. Su versión {netVersion.Version}");
                    return;
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