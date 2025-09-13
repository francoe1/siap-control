using Microsoft.Data.Sqlite;
using SiapControl.Data;
using SiapControl.Data.Models;
using Xunit;

namespace SiapControl.Tests.Data
{
    public class UserRepositoryTests
    {
        [Fact]
        public void InsertAndFindUser()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE Users (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    User TEXT NOT NULL,
                                    Path TEXT NOT NULL
                                );";
                cmd.ExecuteNonQuery();
            }

            var repo = new UserRepository(connection);
            var model = new UserModel { User = "test", Path = "/tmp" };
            repo.Insert(model);

            var loaded = repo.FindById(model.Id);

            Assert.NotNull(loaded);
            Assert.Equal("test", loaded!.User);
            Assert.Equal("/tmp", loaded.Path);
        }
    }
}
