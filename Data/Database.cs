using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SiapControl.Data
{
    public static class Database
    {
        private static SqliteConnection _connection;
        public static UserRepository Users { get; private set; }
        public static ModuleRepository UserModules { get; private set; }

        public static Task ConnectAsync()
        {
            return Task.Run(() =>
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "program.db");
                _connection = new SqliteConnection($"Data Source={path}");
                try
                {
                    _connection.Open();
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 26)
                {
                    _connection.Dispose();
                    File.Delete(path);
                    _connection = new SqliteConnection($"Data Source={path}");
                    _connection.Open();
                }

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Users (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            User TEXT NOT NULL,
                                            Path TEXT NOT NULL
                                        );";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Modules (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            UserId INTEGER NOT NULL,
                                            AppName TEXT,
                                            AppVersion TEXT,
                                            LastUpdate TEXT,
                                            FOREIGN KEY(UserId) REFERENCES Users(Id)
                                        );";
                    cmd.ExecuteNonQuery();
                }

                Users = new UserRepository(_connection);
                UserModules = new ModuleRepository(_connection);
            });
        }
    }
}

