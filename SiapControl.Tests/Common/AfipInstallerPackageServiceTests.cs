using System;
using System.IO;
using System.IO.Compression;
using SiapControl.Common;
using Xunit;

namespace SiapControl.Tests.Common
{
    public class AfipInstallerPackageServiceTests
    {
        [Fact]
        public void TryExtractArchive_ExtractsZipSafely()
        {
            string directory = CreateTempDirectory();
            try
            {
                string zipPath = Path.Combine(directory, "outer.zip");
                string extractPath = Path.Combine(directory, "extract");
                Directory.CreateDirectory(extractPath);

                using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntry("inner/setup.exe");
                    archive.CreateEntry("inner/setup.lst");
                }

                Assert.True(AfipInstallerPackageService.TryExtractArchive(zipPath, extractPath));
                Assert.True(File.Exists(Path.Combine(extractPath, "inner", "setup.exe")));
                Assert.True(File.Exists(Path.Combine(extractPath, "inner", "setup.lst")));
            }
            finally
            {
                DeleteDirectory(directory);
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
