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
                LastUpdate = reader.IsDBNull(4) ? DateTime.MinValue : DateTime.Parse(reader.GetString(4))
            };
        }

        public ModuleModel FindByUserAndAppName(int userId, string appName)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT Id, UserId, AppName, AppVersion, LastUpdate FROM Modules WHERE UserId=@userId AND AppName=@appName";
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
            cmd.CommandText = "SELECT Id, UserId, AppName, AppVersion, LastUpdate FROM Modules WHERE UserId=@userId AND lower(AppName) LIKE @name";
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@name", "%" + name.ToLower() + "%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return ReadModule(reader);
            }
        }

        public void Insert(ModuleModel module)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Modules (UserId, AppName, AppVersion, LastUpdate) VALUES (@userId, @appName, @appVersion, @lastUpdate); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@userId", module.UserId);
            cmd.Parameters.AddWithValue("@appName", module.AppName);
            cmd.Parameters.AddWithValue("@appVersion", module.AppVersion);
            cmd.Parameters.AddWithValue("@lastUpdate", module.LastUpdate.ToString("o"));
            module.Id = Convert.ToInt32((long)cmd.ExecuteScalar());
        }

        public void Update(ModuleModel module)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "UPDATE Modules SET UserId=@userId, AppName=@appName, AppVersion=@appVersion, LastUpdate=@lastUpdate WHERE Id=@id";
            cmd.Parameters.AddWithValue("@userId", module.UserId);
            cmd.Parameters.AddWithValue("@appName", module.AppName);
            cmd.Parameters.AddWithValue("@appVersion", module.AppVersion);
            cmd.Parameters.AddWithValue("@lastUpdate", module.LastUpdate.ToString("o"));
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
    }
}

