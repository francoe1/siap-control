using System;
using System.IO;
using System.Linq;
using SiapControl.Common;
using Xunit;

namespace SiapControl.Tests.Common
{
    public class SiapReaderTests
    {
        [Fact]
        public void Constructor_SetsErrorWhenSiapExecutableDoesNotExist()
        {
            var directory = CreateTempDirectory();
            try
            {
                var reader = new SiapReader(directory);

                Assert.True(reader.HasError);
                Assert.Null(reader.Modules);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public void Constructor_LoadsOnlyModulesWhoseExecutableMatchesFolderName()
        {
            var directory = CreateTempDirectory();
            try
            {
                File.WriteAllText(Path.Combine(directory, "siap.exe"), string.Empty);
                var ivaPath = Path.Combine(directory, "IVA");
                var otherPath = Path.Combine(directory, "Otros");
                Directory.CreateDirectory(ivaPath);
                Directory.CreateDirectory(otherPath);
                File.WriteAllText(Path.Combine(ivaPath, "IVA.exe"), string.Empty);
                File.WriteAllText(Path.Combine(ivaPath, "NoCoincide.exe"), string.Empty);
                File.WriteAllText(Path.Combine(otherPath, "OtroNombre.exe"), string.Empty);

                var reader = new SiapReader(directory);

                Assert.False(reader.HasError);
                var module = Assert.Single(reader.Modules);
                Assert.Equal("IVA", module.AppName);
                Assert.Equal("undefined", module.AppVersion);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public void GetModuleModel_UsesFileNameWhenVersionInfoDoesNotContainProductName()
        {
            var directory = CreateTempDirectory();
            try
            {
                var file = Path.Combine(directory, "Ganancias.exe");
                File.WriteAllText(file, string.Empty);

                var module = SiapReader.GetModuleModel(file);

                Assert.Equal("Ganancias", module.AppName);
                Assert.Equal("undefined", module.AppVersion);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public void GetModuleModel_UsesIconFileNameAsApplicationName()
        {
            var directory = CreateTempDirectory();
            try
            {
                var moduleDirectory = Path.Combine(directory, "IVA");
                Directory.CreateDirectory(moduleDirectory);
                var file = Path.Combine(moduleDirectory, "IVA.exe");
                File.WriteAllText(file, string.Empty);
                File.WriteAllText(Path.Combine(moduleDirectory, "IVA Correcto.ico"), string.Empty);

                var module = SiapReader.GetModuleModel(file);

                Assert.Equal("IVA Correcto", module.AppName);
                Assert.Equal("IVA Correcto", module.IconName);
                Assert.Equal("IVA", module.ExecutableName);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static string CreateTempDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), "SiapControl.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
