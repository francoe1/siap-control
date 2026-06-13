using System;
using System.IO;
using SiapControl.Common;
using Xunit;

namespace SiapControl.Tests.Common
{
    public class SiapInstallationFinderTests
    {
        [Fact]
        public void FindAll_ReturnsRecursiveFoldersWithSiapExeAndAfipDatabase()
        {
            string root = CreateTempDirectory();

            try
            {
                string installation = Path.Combine(root, "Cliente", "SIAP");
                Directory.CreateDirectory(installation);
                File.WriteAllText(Path.Combine(installation, "siap.exe"), string.Empty);
                File.WriteAllText(Path.Combine(installation, "AFIP.MDB"), string.Empty);

                var results = SiapInstallationFinder.FindAll(root);

                var result = Assert.Single(results);
                Assert.Equal("SIAP", result.Name);
                Assert.Equal(installation, result.Path);
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Fact]
        public void FindAll_IgnoresFoldersMissingRequiredFiles()
        {
            string root = CreateTempDirectory();

            try
            {
                string onlyExecutable = Path.Combine(root, "SoloExe");
                string onlyDatabase = Path.Combine(root, "SoloDb");
                Directory.CreateDirectory(onlyExecutable);
                Directory.CreateDirectory(onlyDatabase);
                File.WriteAllText(Path.Combine(onlyExecutable, "siap.exe"), string.Empty);
                File.WriteAllText(Path.Combine(onlyDatabase, "AFIP.MDB"), string.Empty);

                var results = SiapInstallationFinder.FindAll(root);

                Assert.Empty(results);
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Fact]
        public void FindAll_ReturnsEmptyWhenRootDoesNotExist()
        {
            string root = Path.Combine(Path.GetTempPath(), "SiapControl.Tests", Guid.NewGuid().ToString("N"));

            var results = SiapInstallationFinder.FindAll(root);

            Assert.Empty(results);
        }

        [Fact]
        public void FindAll_MatchesFileNamesIgnoringCase()
        {
            string root = CreateTempDirectory();

            try
            {
                File.WriteAllText(Path.Combine(root, "SIAP.EXE"), string.Empty);
                File.WriteAllText(Path.Combine(root, "afip.mdb"), string.Empty);

                var results = SiapInstallationFinder.FindAll(root);

                Assert.Single(results);
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static string CreateTempDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SiapControl.Tests", Guid.NewGuid().ToString("N"));
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
