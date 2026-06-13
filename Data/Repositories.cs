using Microsoft.Data.Sqlite;
using SiapControl.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SiapControl.Data
{
    public class UserRepository
    {
        private readonly SqliteConnection _connection;

        public UserRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        private static UserModel ReadUser(SqliteDataReader reader)
        {
            return new UserModel
            {
                Id = reader.GetInt32(0),
                User = reader.GetString(1),
                Path = reader.GetString(2)
            };
        }

        public IEnumerable<UserModel> FindAll()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, User, Path FROM Users";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return ReadUser(reader);
            }
        }

        public UserModel? FindById(int id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, User, Path FROM Users WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return ReadUser(reader);
            }
            return null;
        }

        public void Insert(UserModel user)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Users (User, Path) VALUES (@user, @path); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@user", user.User);
            cmd.Parameters.AddWithValue("@path", user.Path);
            user.Id = Convert.ToInt32((long)cmd.ExecuteScalar());
        }

        public void Update(UserModel user)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE Users SET User=@user, Path=@path WHERE Id=@id";
            cmd.Parameters.AddWithValue("@user", user.User);
            cmd.Parameters.AddWithValue("@path", user.Path);
            cmd.Parameters.AddWithValue("@id", user.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }

    public class ModuleRepository
    {
        private readonly SqliteConnection _connection;

        public ModuleRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        private ModuleModel ReadModule(SqliteDataReader reader)
        {
            return new ModuleModel
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                AppName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                AppVersion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                LastUpdate = reader.IsDBNull(4) ? DateTime.MinValue : DateTime.Parse(reader.GetString(4)),
                ExecutableName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                IconName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                ProductName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                FileDescription = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                InternalName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                OriginalFilename = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                CompanyName = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                Comments = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                FileVersion = reader.IsDBNull(13) ? string.Empty : reader.GetString(13)
            };
        }

        public ModuleModel FindByUserAndAppName(int userId, string appName)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = SelectModulesSql + " WHERE UserId=@userId AND AppName=@appName";
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@appName", appName);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return ReadModule(reader);
            }
            return null;
        }

        public IEnumerable<ModuleModel> FindByUserAndName(int userId, string name)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = SelectModulesSql + @" WHERE UserId=@userId AND (
                                    lower(coalesce(AppName, '')) LIKE @name OR
                                    lower(coalesce(IconName, '')) LIKE @name OR
                                    lower(coalesce(ProductName, '')) LIKE @name OR
                                    lower(coalesce(FileDescription, '')) LIKE @name OR
                                    lower(coalesce(InternalName, '')) LIKE @name OR
                                    lower(coalesce(OriginalFilename, '')) LIKE @name OR
                                    lower(coalesce(ExecutableName, '')) LIKE @name
                                )";
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@name", "%" + name.ToLower() + "%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return ReadModule(reader);
            }
        }

        public IEnumerable<ModuleModel> FindAll()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = SelectModulesSql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return ReadModule(reader);
            }
        }

        public void Insert(ModuleModel module)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO Modules
                                    (UserId, AppName, AppVersion, LastUpdate, ExecutableName, IconName, ProductName, FileDescription, InternalName, OriginalFilename, CompanyName, Comments, FileVersion)
                                VALUES
                                    (@userId, @appName, @appVersion, @lastUpdate, @executableName, @iconName, @productName, @fileDescription, @internalName, @originalFilename, @companyName, @comments, @fileVersion);
                                SELECT last_insert_rowid();";
            AddModuleParameters(cmd, module);
            module.Id = Convert.ToInt32((long)cmd.ExecuteScalar());
        }

        public void Update(ModuleModel module)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"UPDATE Modules SET
                                    UserId=@userId,
                                    AppName=@appName,
                                    AppVersion=@appVersion,
                                    LastUpdate=@lastUpdate,
                                    ExecutableName=@executableName,
                                    IconName=@iconName,
                                    ProductName=@productName,
                                    FileDescription=@fileDescription,
                                    InternalName=@internalName,
                                    OriginalFilename=@originalFilename,
                                    CompanyName=@companyName,
                                    Comments=@comments,
                                    FileVersion=@fileVersion
                                WHERE Id=@id";
            AddModuleParameters(cmd, module);
            cmd.Parameters.AddWithValue("@id", module.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Modules WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteByUser(int userId)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Modules WHERE UserId=@userId";
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.ExecuteNonQuery();
        }

        private const string SelectModulesSql = @"SELECT Id, UserId, AppName, AppVersion, LastUpdate,
                                                        ExecutableName, IconName, ProductName, FileDescription,
                                                        InternalName, OriginalFilename, CompanyName, Comments, FileVersion
                                                  FROM Modules";

        private static void AddModuleParameters(SqliteCommand cmd, ModuleModel module)
        {
            cmd.Parameters.AddWithValue("@userId", module.UserId);
            cmd.Parameters.AddWithValue("@appName", module.AppName ?? string.Empty);
            cmd.Parameters.AddWithValue("@appVersion", module.AppVersion ?? string.Empty);
            cmd.Parameters.AddWithValue("@lastUpdate", module.LastUpdate.ToString("o"));
            cmd.Parameters.AddWithValue("@executableName", module.ExecutableName ?? string.Empty);
            cmd.Parameters.AddWithValue("@iconName", module.IconName ?? string.Empty);
            cmd.Parameters.AddWithValue("@productName", module.ProductName ?? string.Empty);
            cmd.Parameters.AddWithValue("@fileDescription", module.FileDescription ?? string.Empty);
            cmd.Parameters.AddWithValue("@internalName", module.InternalName ?? string.Empty);
            cmd.Parameters.AddWithValue("@originalFilename", module.OriginalFilename ?? string.Empty);
            cmd.Parameters.AddWithValue("@companyName", module.CompanyName ?? string.Empty);
            cmd.Parameters.AddWithValue("@comments", module.Comments ?? string.Empty);
            cmd.Parameters.AddWithValue("@fileVersion", module.FileVersion ?? string.Empty);
        }
    }

    public class AutoUpdateSettingsRepository
    {
        private const int SettingsId = 1;
        private readonly SqliteConnection _connection;

        public AutoUpdateSettingsRepository(SqliteConnection connection)
        {
            _connection = connection;
        }

        public AutoUpdateSettingsModel Get()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"SELECT AutoUpdateEnabled, ScheduledDaysMask, ScheduledTime, StartWithWindows, LastAutoUpdateRun
                                FROM AutoUpdateSettings
                                WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", SettingsId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                var settings = CreateDefault();
                Save(settings);
                return settings;
            }

            return new AutoUpdateSettingsModel
            {
                AutoUpdateEnabled = reader.GetInt32(0) == 1,
                ScheduledDaysMask = reader.GetInt32(1),
                ScheduledTime = TimeSpan.TryParse(reader.GetString(2), out TimeSpan time) ? time : new TimeSpan(20, 0, 0),
                StartWithWindows = reader.GetInt32(3) == 1,
                LastAutoUpdateRun = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4))
            };
        }

        public void Save(AutoUpdateSettingsModel settings)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO AutoUpdateSettings
                                    (Id, AutoUpdateEnabled, ScheduledDaysMask, ScheduledTime, StartWithWindows, LastAutoUpdateRun)
                                VALUES
                                    (@id, @enabled, @days, @time, @startup, @lastRun)
                                ON CONFLICT(Id) DO UPDATE SET
                                    AutoUpdateEnabled=excluded.AutoUpdateEnabled,
                                    ScheduledDaysMask=excluded.ScheduledDaysMask,
                                    ScheduledTime=excluded.ScheduledTime,
                                    StartWithWindows=excluded.StartWithWindows,
                                    LastAutoUpdateRun=excluded.LastAutoUpdateRun";
            cmd.Parameters.AddWithValue("@id", SettingsId);
            cmd.Parameters.AddWithValue("@enabled", settings.AutoUpdateEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@days", settings.ScheduledDaysMask);
            cmd.Parameters.AddWithValue("@time", settings.ScheduledTime.ToString(@"hh\:mm"));
            cmd.Parameters.AddWithValue("@startup", settings.StartWithWindows ? 1 : 0);
            cmd.Parameters.AddWithValue("@lastRun", settings.LastAutoUpdateRun.HasValue ? settings.LastAutoUpdateRun.Value.ToString("o") : (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void UpdateLastRun(DateTime lastRun)
        {
            AutoUpdateSettingsModel settings = Get();
            settings.LastAutoUpdateRun = lastRun;
            Save(settings);
        }

        private static AutoUpdateSettingsModel CreateDefault()
        {
            return new AutoUpdateSettingsModel
            {
                AutoUpdateEnabled = false,
                ScheduledDaysMask = (1 << (int)DayOfWeek.Monday) |
                                    (1 << (int)DayOfWeek.Tuesday) |
                                    (1 << (int)DayOfWeek.Wednesday) |
                                    (1 << (int)DayOfWeek.Thursday) |
                                    (1 << (int)DayOfWeek.Friday),
                ScheduledTime = new TimeSpan(20, 0, 0),
                StartWithWindows = false
            };
        }
    }
}

