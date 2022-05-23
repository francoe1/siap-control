using LiteDB;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SiapControl
{
    public class DbContext
    {
        private LiteDatabase m_connection { get; }

        public static ILiteCollection<UserModel> Users { get; private set; }
        public static ILiteCollection<UpdateRegister> UpdateRegisters { get; private set; }
        public static ILiteCollection<ModuleModel> UserModules { get; private set; }

        public static DbContext Instance { get; private set; }

        public DbContext()
        {
            Instance = this;
            string conf = $@"{AppDomain.CurrentDomain.BaseDirectory}db.conf";
            string path = $@"{AppDomain.CurrentDomain.BaseDirectory}program.db";
            if (!File.Exists(conf)) File.WriteAllText(conf, path);
            else path = File.ReadAllText(conf);

            if (!File.Exists(path))
            {
                using (OpenFileDialog folder = new OpenFileDialog())
                {
                    if (folder.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folder.FileName))
                        path = folder.FileName;
                }
            }

            m_connection = new LiteDatabase(path);
            Users = m_connection.GetCollection<UserModel>();
            UpdateRegisters = m_connection.GetCollection<UpdateRegister>();
            UserModules = m_connection.GetCollection<ModuleModel>();
        }

        public static void ExportToJson()
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
        }
    }
}