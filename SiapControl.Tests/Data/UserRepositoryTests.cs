using System.Linq;
using SiapControl.Data;
using SiapControl.Data.Models;
using Xunit;

namespace SiapControl.Tests.Data
{
    public class UserRepositoryTests
    {
        [Fact]
        public void InsertAndFindById_SavesUser()
        {
            using var database = new RepositoryTestDatabase();
            var repo = new UserRepository(database.Connection);
            var model = new UserModel { User = "test", Path = @"C:\SIAP" };

            repo.Insert(model);

            var loaded = repo.FindById(model.Id);
            Assert.NotNull(loaded);
            Assert.Equal("test", loaded!.User);
            Assert.Equal(@"C:\SIAP", loaded.Path);
        }

        [Fact]
        public void FindAll_ReturnsInsertedUsers()
        {
            using var database = new RepositoryTestDatabase();
            var repo = new UserRepository(database.Connection);
            repo.Insert(new UserModel { User = "uno", Path = @"C:\SIAP1" });
            repo.Insert(new UserModel { User = "dos", Path = @"C:\SIAP2" });

            var users = repo.FindAll().ToArray();

            Assert.Equal(2, users.Length);
            Assert.Contains(users, user => user.User == "uno");
            Assert.Contains(users, user => user.User == "dos");
        }

        [Fact]
        public void Update_ChangesExistingUser()
        {
            using var database = new RepositoryTestDatabase();
            var repo = new UserRepository(database.Connection);
            var model = new UserModel { User = "antes", Path = @"C:\Antes" };
            repo.Insert(model);

            model.User = "despues";
            model.Path = @"D:\Despues";
            repo.Update(model);

            var loaded = repo.FindById(model.Id);
            Assert.Equal("despues", loaded!.User);
            Assert.Equal(@"D:\Despues", loaded.Path);
        }

        [Fact]
        public void Delete_RemovesUser()
        {
            using var database = new RepositoryTestDatabase();
            var repo = new UserRepository(database.Connection);
            var model = new UserModel { User = "borrar", Path = @"C:\SIAP" };
            repo.Insert(model);

            repo.Delete(model.Id);

            Assert.Null(repo.FindById(model.Id));
        }

        [Fact]
        public void FindById_ReturnsNullWhenUserDoesNotExist()
        {
            using var database = new RepositoryTestDatabase();
            var repo = new UserRepository(database.Connection);

            Assert.Null(repo.FindById(999));
        }
    }
}
