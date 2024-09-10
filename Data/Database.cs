using LiteDB;
using SiapControl.Data.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SiapControl.Data
{
    public static class Database
    {
        private static LiteDatabase _connection { get; set; }
        public static ILiteCollection<UserModel> Users { get; private set; }
        public static ILiteCollection<ModuleModel> UserModules { get; private set; }

        public static Task ConnectAsync()
        {
            return Task.Run(() =>
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "program.db");
                _connection = new LiteDatabase(path);
                Users = _connection.GetCollection<UserModel>();
                UserModules = _connection.GetCollection<ModuleModel>();
            });
        }
    }
}