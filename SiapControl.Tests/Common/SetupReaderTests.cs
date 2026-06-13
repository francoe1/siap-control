using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SiapControl.Common;
using Xunit;

namespace SiapControl.Tests.Common
{
    public class SetupReaderTests
    {
        [Fact]
        public void Open_LoadsApplicationNameVersionAndParametersFromLst()
        {
            var directory = CreateTempDirectory();
            try
            {
                var setupPath = Path.Combine(directory, "setup.exe");
                var lstPath = Path.Combine(directory, "setup.lst");
                File.WriteAllText(setupPath, string.Empty);
                File.WriteAllLines(lstPath, new[]
                {
                    "AppExe=IVA.exe",
                    "VersionApp=\"2.5.1\"",
                    "Title=Instalador IVA"
                });

                var reader = new SetupReader(setupPath);

                Assert.True(reader.Open());
                Assert.Equal("IVA", reader.AppName);
                Assert.Equal("2.5.1", reader.AppVersion);
                Assert.Contains(reader.Parameters, parameter => parameter.Name == "Title" && parameter.Value == "Instalador IVA");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public void CreateBackup_CreatesZipForApplicationFolder()
        {
            var directory = CreateTempDirectory();
            try
            {
                var setupPath = Path.Combine(directory, "setup.exe");
                var lstPath = Path.Combine(directory, "setup.lst");
                var userPath = Path.Combine(directory, "Usuario");
                var appPath = Path.Combine(userPath, "IVA");
                Directory.CreateDirectory(appPath);
                File.WriteAllText(setupPath, string.Empty);
                File.WriteAllLines(lstPath, new[] { "AppExe=IVA.exe", "VersionApp=\"1.0\"", "Title=Instalador IVA" });
                File.WriteAllText(Path.Combine(appPath, "IVA.exe"), "contenido");

                var reader = new SetupReader(setupPath);
                Assert.True(reader.Open());

                reader.CreateBackup(userPath);

                var zipPath = Path.Combine(userPath, "_backup", "IVA.zip");
                Assert.True(File.Exists(zipPath));
                using var archive = ZipFile.OpenRead(zipPath);
                Assert.Contains(archive.Entries, entry => entry.FullName == "IVA.exe");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        [Fact]
        public void CreateBackup_DoesNothingWhenApplicationFolderDoesNotExist()
        {
            var directory = CreateTempDirectory();
            try
            {
                var setupPath = Path.Combine(directory, "setup.exe");
                var lstPath = Path.Combine(directory, "setup.lst");
                var userPath = Path.Combine(directory, "Usuario");
                Directory.CreateDirectory(userPath);
                File.WriteAllText(setupPath, string.Empty);
                File.WriteAllLines(lstPath, new[] { "AppExe=IVA.exe", "VersionApp=\"1.0\"" });

                var reader = new SetupReader(setupPath);
                Assert.True(reader.Open());

                reader.CreateBackup(userPath);

                Assert.False(File.Exists(Path.Combine(userPath, "_backup", "IVA.zip")));
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
