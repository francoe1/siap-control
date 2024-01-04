using LiteDB;
using Newtonsoft.Json;
using SiapControl.Data.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SiapControl.Data
{
    public static class Database
    {
        private static LiteDatabase _connection { get; set; }

        public static ILiteCollection<UserModel> Users { get; private set; }
        public static ILiteCollection<UpdateRegisterModel> UpdateRegisters { get; private set; }
        public static ILiteCollection<ModuleModel> UserModules { get; private set; }

        public static Task ConnectAsync()
        {
            return Task.Run(() =>
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "program.db");
                _connection = new LiteDatabase(path);
                Users = _connection.GetCollection<UserModel>();
                UpdateRegisters = _connection.GetCollection<UpdateRegisterModel>();
                UserModules = _connection.GetCollection<ModuleModel>();
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