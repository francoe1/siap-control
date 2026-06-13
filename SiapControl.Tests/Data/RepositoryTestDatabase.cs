using System;
using Microsoft.Data.Sqlite;

namespace SiapControl.Tests.Data
{
    internal sealed class RepositoryTestDatabase : IDisposable
    {
        public RepositoryTestDatabase(bool seedUsers = false)
        {
            Connection = new SqliteConnection("Data Source=:memory:");
            Connection.Open();
            CreateSchema(seedUsers);
        }

        public SqliteConnection Connection { get; }

        public void Dispose()
        {
            Connection.Dispose();
        }

        private void CreateSchema(bool seedUsers)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE Users (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        User TEXT NOT NULL,
                                        Path TEXT NOT NULL
                                    );";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE Modules (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        UserId INTEGER NOT NULL,
                                        AppName TEXT,
                                        AppVersion TEXT,
                                        LastUpdate TEXT,
                                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                                    );";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE AutoUpdateSettings (
                                        Id INTEGER PRIMARY KEY CHECK (Id = 1),
                                        AutoUpdateEnabled INTEGER NOT NULL,
                                        ScheduledDaysMask INTEGER NOT NULL,
                                        ScheduledTime TEXT NOT NULL,
                                        StartWithWindows INTEGER NOT NULL,
                                        LastAutoUpdateRun TEXT
                                    );";
                cmd.ExecuteNonQuery();
            }

            if (!seedUsers)
            {
                return;
            }

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO Users (User, Path) VALUES
                                    ('usuario uno', 'C:\SIAP1'),
                                    ('usuario dos', 'C:\SIAP2');";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
