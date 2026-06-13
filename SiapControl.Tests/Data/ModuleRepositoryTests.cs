using System;
using System.Linq;
using SiapControl.Data;
using SiapControl.Data.Models;
using Xunit;

namespace SiapControl.Tests.Data
{
    public class ModuleRepositoryTests
    {
        [Fact]
        public void InsertAndFindByUserAndAppName_SavesModule()
        {
            using var database = new RepositoryTestDatabase(seedUsers: true);
            var repo = new ModuleRepository(database.Connection);
            var module = CreateModule("IVA", "1.2.3");

            repo.Insert(module);

            var loaded = repo.FindByUserAndAppName(1, "IVA");
            Assert.NotNull(loaded);
            Assert.Equal("IVA", loaded.AppName);
            Assert.Equal("1.2.3", loaded.AppVersion);
            Assert.Equal("ivaexe", loaded.ExecutableName);
            Assert.Equal("IVA Icono", loaded.IconName);
            Assert.Equal("Producto IVA", loaded.ProductName);
            Assert.Equal(module.LastUpdate, loaded.LastUpdate);
        }

        [Fact]
        public void FindByUserAndAppName_ReturnsNullWhenModuleDoesNotExist()
        {
            using var database = new RepositoryTestDatabase(seedUsers: true);
            var repo = new ModuleRepository(database.Connection);

            Assert.Null(repo.FindByUserAndAppName(1, "Ganancias"));
        }

        [Fact]
        public void FindByUserAndName_FiltersByUserAndNameIgnoringCase()
        {
            using var database = new RepositoryTestDatabase(seedUsers: true);
            var repo = new ModuleRepository(database.Connection);
            repo.Insert(CreateModule("Ganancias Personas", "1", userId: 1));
            repo.Insert(CreateModule("IVA", "1", userId: 1));
            repo.Insert(CreateModule("Ganancias Sociedades", "1", userId: 2));

            var modules = repo.FindByUserAndName(1, "ganancias").ToArray();

            Assert.Single(modules);
            Assert.Equal("Ganancias Personas", modules[0].AppName);
        }

        [Fact]
        public void Update_ChangesExistingModule()
        {
            using var database = new RepositoryTestDatabase(seedUsers: true);
            var repo = new ModuleRepository(database.Connection);
            var module = CreateModule("IVA", "1.0.0");
            repo.Insert(module);

            module.AppVersion = "2.0.0";
            module.LastUpdate = new DateTime(2024, 2, 3, 4, 5, 6);
            repo.Update(module);

            var loaded = repo.FindByUserAndAppName(1, "IVA");
            Assert.Equal("2.0.0", loaded.AppVersion);
            Assert.Equal(module.LastUpdate, loaded.LastUpdate);
        }

        [Fact]
        public void Delete_RemovesModule()
        {
            using var database = new RepositoryTestDatabase(seedUsers: true);
            var repo = new ModuleRepository(database.Connection);
            var module = CreateModule("IVA", "1");
            repo.Insert(module);

            repo.Delete(module.Id);

            Assert.Null(repo.FindByUserAndAppName(1, "IVA"));
        }

        [Fact]
        public void DeleteByUser_RemovesOnlyModulesForThatUser()
        {
            using var database = new RepositoryTestDatabase(seedUsers: true);
            var repo = new ModuleRepository(database.Connection);
            repo.Insert(CreateModule("IVA", "1", userId: 1));
            repo.Insert(CreateModule("Ganancias", "1", userId: 2));

            repo.DeleteByUser(1);

            Assert.Empty(repo.FindByUserAndName(1, string.Empty));
            Assert.Single(repo.FindByUserAndName(2, string.Empty));
        }

        [Fact]
        public void ReadModule_MapsNullColumnsToSafeDefaults()
        {
            using var database = new RepositoryTestDatabase(seedUsers: true);
            using (var cmd = database.Connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Modules (UserId, AppName, AppVersion, LastUpdate) VALUES (1, 'SinDatos', NULL, NULL)";
                cmd.ExecuteNonQuery();
            }

            var repo = new ModuleRepository(database.Connection);
            var module = repo.FindByUserAndName(1, "SinDatos").Single();

            Assert.Equal("SinDatos", module.AppName);
            Assert.Equal(string.Empty, module.AppVersion);
            Assert.Equal(DateTime.MinValue, module.LastUpdate);
            Assert.Equal(string.Empty, module.IconName);
            Assert.Equal(string.Empty, module.ProductName);
        }

        private static ModuleModel CreateModule(string appName, string version, int userId = 1)
        {
            return new ModuleModel
            {
                UserId = userId,
                AppName = appName,
                AppVersion = version,
                ExecutableName = appName.ToLowerInvariant() + "exe",
                IconName = appName + " Icono",
                ProductName = "Producto " + appName,
                FileDescription = "Descripcion " + appName,
                InternalName = "Interno " + appName,
                OriginalFilename = appName + ".exe",
                CompanyName = "AFIP",
                Comments = "Comentario " + appName,
                FileVersion = version,
                LastUpdate = new DateTime(2024, 1, 2, 3, 4, 5)
            };
        }
    }
}
