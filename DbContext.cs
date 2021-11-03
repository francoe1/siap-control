using LiteDB;
using System;
using System.IO;
using System.Windows.Forms;

namespace SiapControl
{
    public class DbContext
    {
        private LiteDatabase m_connection { get; }

        public static ILiteCollection<UserModel> Users { get; private set; }
        public static ILiteCollection<UpdateRegister> UpdateRegisters { get; private set; }
        public static ILiteCollection<ModuleModel> UserModules { get; private set; }

        public DbContext()
        {
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
    }
}
