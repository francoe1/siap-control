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
        Task<SetupReader?> DownloadAndPrepareAsync(AfipApplicationPackage package, IProgress<AfipPackageProgress>? progress);
    }

    public sealed class AfipPackageProgress
    {
        public AfipPackageProgress(string message, int percentage)
        {
            Message = message;
            Percentage = percentage;
        }

        public string Message { get; }
        public int Percentage { get; }
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
            return await DownloadAndPrepareAsync(package, null);
        }

        public async Task<SetupReader?> DownloadAndPrepareAsync(AfipApplicationPackage package, IProgress<AfipPackageProgress>? progress)
        {
            string root = Path.Combine(Path.GetTempPath(), "SiapControl", "Autoupdater", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            string firstZip = Path.Combine(root, "download.zip");
            progress?.Report(new AfipPackageProgress($"Descargando {package.DisplayName}...", 0));
            await DownloadFileAsync(package.DownloadUri, firstZip, progress, package.DisplayName);

            string extractRoot = Path.Combine(root, "extract");
            Directory.CreateDirectory(extractRoot);
            progress?.Report(new AfipPackageProgress($"Descomprimiendo {package.DisplayName}...", 65));
            if (!TryExtractArchive(firstZip, extractRoot))
            {
                return null;
            }

            progress?.Report(new AfipPackageProgress("Buscando setup.exe...", 75));
            string? setupPath = FindSetup(extractRoot);
            for (int depth = 0; setupPath == null && depth < 3; depth++)
            {
                string[] candidates = Directory.GetFiles(extractRoot, "*.*", SearchOption.AllDirectories)
                    .Where(file => file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                bool extractedAny = false;
                foreach (string candidate in candidates)
                {
                    progress?.Report(new AfipPackageProgress($"Descomprimiendo paquete interno {Path.GetFileName(candidate)}...", 80 + depth * 5));
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

            progress?.Report(new AfipPackageProgress("Leyendo metadata del instalador...", 95));
            var reader = new SetupReader(setupPath);
            SetupReader? setup = reader.Open() ? reader : null;
            progress?.Report(new AfipPackageProgress(setup == null ? "No se pudo abrir el instalador." : "Instalador preparado.", setup == null ? 0 : 100));
            return setup;
        }

        private async Task DownloadFileAsync(Uri uri, string destination, IProgress<AfipPackageProgress>? progress, string displayName)
        {
            using HttpResponseMessage response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            using Stream source = await response.Content.ReadAsStreamAsync();
            using FileStream target = File.Create(destination);
            byte[] buffer = new byte[81920];
            long totalRead = 0;

            while (true)
            {
                int read = await source.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                await target.WriteAsync(buffer, 0, read);
                totalRead += read;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    int percentage = Math.Min(60, (int)Math.Round(totalRead * 60d / totalBytes.Value));
                    progress?.Report(new AfipPackageProgress($"Descargando {displayName}: {FormatBytes(totalRead)} de {FormatBytes(totalBytes.Value)}", percentage));
                }
                else
                {
                    progress?.Report(new AfipPackageProgress($"Descargando {displayName}: {FormatBytes(totalRead)}", 30));
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return (bytes / 1024d / 1024d).ToString("0.0") + " MB";
            }

            if (bytes >= 1024)
            {
                return (bytes / 1024d).ToString("0.0") + " KB";
            }

            return bytes + " B";
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
