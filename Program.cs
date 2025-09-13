using SiapControl.Common;
using SiapControl.Forms;
using System;
using System.Diagnostics;
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
                    //MessageBox.Show("Necesitas permisos de administrador", "Error");
                    //RestartAsAdministrator();
                }

                Application.Run(new ControlForm());
            }
            catch (Exception ex)
            {
                Exception inner = ex;
                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }
                MessageBox.Show($"{inner.Message} ---> \n{inner.StackTrace}\n{inner.Source}\n{inner.TargetSite}", "Error");
            }
        }

        private static void RestartAsAdministrator()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas" 
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al reiniciar la aplicación con privilegios elevados: " + ex.Message);
            }

            // Finaliza la aplicación actual
            Environment.Exit(0);
        }
    }
}