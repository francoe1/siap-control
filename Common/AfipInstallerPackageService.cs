using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SiapControl.Common
{
    public interface IAfipInstallerPackageService
    {
        Task<SetupReader?> DownloadAndPrepareAsync(AfipApplicationPackage package);
    }

    public sealed class AfipInstallerPackageService : IAfipInstallerPackageService
    {
        private readonly HttpClient _client;

        public AfipInstallerPackageService()
            : this(CreateClient())
        {
        }

        public AfipInstallerPackageService(HttpClient client)
        {
            _client = client;
        }

        public async Task<SetupReader?> DownloadAndPrepareAsync(AfipApplicationPackage package)
        {
            string root = Path.Combine(Path.GetTempPath(), "SiapControl", "Autoupdater", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            string firstZip = Path.Combine(root, "download.zip");
            byte[] bytes = await _client.GetByteArrayAsync(package.DownloadUri);
            File.WriteAllBytes(firstZip, bytes);

            string extractRoot = Path.Combine(root, "extract");
            Directory.CreateDirectory(extractRoot);
            if (!TryExtractArchive(firstZip, extractRoot))
            {
                return null;
            }

            string? setupPath = FindSetup(extractRoot);
            for (int depth = 0; setupPath == null && depth < 3; depth++)
            {
                string[] candidates = Directory.GetFiles(extractRoot, "*.*", SearchOption.AllDirectories)
                    .Where(file => file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                bool extractedAny = false;
                foreach (string candidate in candidates)
                {
                    string nestedTarget = Path.Combine(root, "nested-" + depth, Path.GetFileNameWithoutExtension(candidate));
                    Directory.CreateDirectory(nestedTarget);
                    if (TryExtractArchive(candidate, nestedTarget))
                    {
                        extractedAny = true;
                    }
                }

                if (!extractedAny)
                {
                    break;
                }

                setupPath = FindSetup(root);
            }

            if (setupPath == null)
            {
                return null;
            }

            var reader = new SetupReader(setupPath);
            return reader.Open() ? reader : null;
        }

        public static bool TryExtractArchive(string archivePath, string destination)
        {
            try
            {
                using ZipArchive archive = ZipFile.OpenRead(archivePath);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string targetPath = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                    string destinationRoot = Path.GetFullPath(destination);
                    if (!targetPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(targetPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    entry.ExtractToFile(targetPath, true);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? FindSetup(string root)
        {
            foreach (string setup in Directory.GetFiles(root, "setup.exe", SearchOption.AllDirectories))
            {
                string lst = Path.Combine(Path.GetDirectoryName(setup)!, "setup.lst");
                if (File.Exists(lst))
                {
                    return setup;
                }
            }

            return null;
        }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SiapControl-AfipAutoupdater");
            return client;
        }
    }
}
