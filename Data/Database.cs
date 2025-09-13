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

        public static async Task ConnectAsync()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "program.db");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            async Task<SqliteConnection> OpenAndInitAsync()
            {
                var conn = new SqliteConnection($"Data Source={path};Pooling=False");
                try
                {
                    await conn.OpenAsync();

                    using (var cmd = conn.CreateCommand())
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

                    return conn;
                }
                catch
                {
                    conn.Dispose();
                    throw;
                }
            }

            try
            {
                _connection = await OpenAndInitAsync();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 26 || ex.SqliteErrorCode == 14)
            {
                // clear any pooled connections so the file handle is released
                SqliteConnection.ClearAllPools();

                if (File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (IOException)
                    {
                        // if another process is using the file, surface a clearer message
                        throw new IOException($"Unable to rebuild database because '{path}' is in use by another process.");
                    }
                }

                _connection = await OpenAndInitAsync();
            }

            Users = new UserRepository(_connection);
            UserModules = new ModuleRepository(_connection);
        }
    }
}

