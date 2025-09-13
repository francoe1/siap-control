using System;
using System.IO;
using System.Windows;
using SiapControl.Data;
using SiapControl.Data.Models;

namespace SiapControl.Common
{
    public static class ModuleUpdater
    {
        public static void UpdateModules(int userId, string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                SiapReader reader = new SiapReader(path);
                Database.UserModules.DeleteByUser(userId);

                if (reader.Modules != null)
                {
                    foreach (ModuleModel module in reader.Modules)
                    {
                        module.UserId = userId;
                        Database.UserModules.Insert(module);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al intentar actualizar el modulo del usuario:{userId} \n{ex.Message}\n{ex.StackTrace}", "Error");
            }
        }
    }
}

