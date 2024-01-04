using LiteDB;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SiapControl
{
    public static class Database
    {
        private static LiteDatabase m_connection { get; set; }

        public static ILiteCollection<UserModel> Users { get; private set; }
        public static ILiteCollection<UpdateRegister> UpdateRegisters { get; private set; }
        public static ILiteCollection<ModuleModel> UserModules { get; private set; }

        public static Task ConnectAsync()
        {
            return Task.Run(() =>
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "program.db");
                m_connection = new LiteDatabase(path);
                Users = m_connection.GetCollection<UserModel>();
                UpdateRegisters = m_connection.GetCollection<UpdateRegister>();
                UserModules = m_connection.GetCollection<ModuleModel>();
            });
        }

        public static Task ExportToJsonAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    string userJson = JsonConvert.SerializeObject(Users.FindAll().ToArray());
                    string modulesJson = JsonConvert.SerializeObject(UserModules.FindAll().ToArray());
                    string output = $@"{AppDomain.CurrentDomain.BaseDirectory}db.json";
                    File.WriteAllText(output, "{" + $"\"users\":{userJson}, \"modules\":{modulesJson}" + "}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error al exportar JSON");
                }
            });
        }
    }
}