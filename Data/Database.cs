using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SiapControl.Data
{
    public sealed class DatabaseInitializationProgress
    {
        public DatabaseInitializationProgress(int percentage, string message)
        {
            Percentage = percentage;
            Message = message;
        }

        public int Percentage { get; }
        public string Message { get; }
    }

    public static class Database
    {
        private static SqliteConnection? _connection;
        public static UserRepository Users { get; private set; } = null!;
        public static ModuleRepository UserModules { get; private set; } = null!;
        public static AutoUpdateSettingsRepository AutoUpdateSettings { get; private set; } = null!;

        public static async Task ConnectAsync(IProgress<DatabaseInitializationProgress>? progress = null)
        {
            if (_connection != null)
            {
                progress?.Report(new DatabaseInitializationProgress(100, "Base de datos lista."));
                return;
            }

            progress?.Report(new DatabaseInitializationProgress(5, "Preparando base de datos..."));

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "program.db");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            static async Task InitializeSchemaAsync(SqliteConnection conn, IProgress<DatabaseInitializationProgress>? schemaProgress)
            {
                schemaProgress?.Report(new DatabaseInitializationProgress(55, "Configurando SQLite..."));

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA foreign_keys = ON;";
                    await cmd.ExecuteNonQueryAsync();
                }

                schemaProgress?.Report(new DatabaseInitializationProgress(70, "Creando tabla de usuarios..."));

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Users (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            User TEXT NOT NULL,
                                            Path TEXT NOT NULL
                                        );";
                    await cmd.ExecuteNonQueryAsync();
                }

                schemaProgress?.Report(new DatabaseInitializationProgress(85, "Creando tabla de modulos..."));

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Modules (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            UserId INTEGER NOT NULL,
                                            AppName TEXT,
                                            AppVersion TEXT,
                                            LastUpdate TEXT,
                                            FOREIGN KEY(UserId) REFERENCES Users(Id)
                                        );";
                    await cmd.ExecuteNonQueryAsync();
                }

                schemaProgress?.Report(new DatabaseInitializationProgress(92, "Creando configuracion de autoupdater..."));

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS AutoUpdateSettings (
                                            Id INTEGER PRIMARY KEY CHECK (Id = 1),
                                            AutoUpdateEnabled INTEGER NOT NULL,
                                            ScheduledDaysMask INTEGER NOT NULL,
                                            ScheduledTime TEXT NOT NULL,
                                            StartWithWindows INTEGER NOT NULL,
                                            LastAutoUpdateRun TEXT
                                        );";
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            async Task<SqliteConnection> OpenAndInitAsync()
            {
                var conn = new SqliteConnection($"Data Source={path};Pooling=False");
                try
                {
                    progress?.Report(new DatabaseInitializationProgress(25, "Abriendo conexion SQLite..."));
                    await conn.OpenAsync();

                    progress?.Report(new DatabaseInitializationProgress(45, "Inicializando esquema..."));
                    await InitializeSchemaAsync(conn, progress);
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
                progress?.Report(new DatabaseInitializationProgress(35, "Reconstruyendo base de datos danada..."));

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
            AutoUpdateSettings = new AutoUpdateSettingsRepository(_connection);
            AutoUpdateSettings.Get();
            progress?.Report(new DatabaseInitializationProgress(100, "Base de datos lista."));
        }
    }
}

