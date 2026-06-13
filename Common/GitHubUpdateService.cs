using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace SiapControl.Common
{
    public sealed class GitHubUpdateService
    {
        private const string Owner = "francoe1";
        private const string Repository = "siap-control";
        private const string ReleaseAssetName = "SiapControl.zip";

        private static readonly Uri LatestReleaseUri = new Uri($"https://api.github.com/repos/{Owner}/{Repository}/releases/latest");

        public async Task<GitHubUpdate?> CheckForUpdateAsync()
        {
            using var client = CreateClient();
            using var stream = await client.GetStreamAsync(LatestReleaseUri);
            var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
            var release = (GitHubRelease?)serializer.ReadObject(stream);

            if (release == null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return null;
            }

            if (!TryParseVersion(release.TagName, out Version latestVersion))
            {
                return null;
            }

            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            if (latestVersion <= currentVersion)
            {
                return null;
            }

            GitHubReleaseAsset? asset = release.Assets?.FirstOrDefault(x => x.Name.Equals(ReleaseAssetName, StringComparison.OrdinalIgnoreCase));
            if (asset == null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            {
                return null;
            }

            return new GitHubUpdate(latestVersion, asset.BrowserDownloadUrl, release.HtmlUrl);
        }

        public async Task DownloadAndInstallAsync(GitHubUpdate update)
        {
            string updateRoot = Path.Combine(Path.GetTempPath(), "SiapControlUpdate", Guid.NewGuid().ToString("N"));
            string zipPath = Path.Combine(updateRoot, ReleaseAssetName);
            string extractPath = Path.Combine(updateRoot, "extract");
            Directory.CreateDirectory(updateRoot);
            Directory.CreateDirectory(extractPath);

            using (var client = CreateClient())
            {
                byte[] bytes = await client.GetByteArrayAsync(update.DownloadUrl);
                File.WriteAllBytes(zipPath, bytes);
            }

            ZipFile.ExtractToDirectory(zipPath, extractPath);
            string scriptPath = CreateInstallScript(updateRoot, extractPath);
            Process.Start(new ProcessStartInfo(scriptPath) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
        }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SiapControl-Updater");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static bool TryParseVersion(string tagName, out Version version)
        {
            string value = tagName.Trim().TrimStart('v', 'V');
            return Version.TryParse(value, out version);
        }

        private static string CreateInstallScript(string updateRoot, string extractPath)
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string appPath = Assembly.GetExecutingAssembly().Location;
            int currentProcessId = Process.GetCurrentProcess().Id;
            string scriptPath = Path.Combine(updateRoot, "install-update.cmd");

            string script = $@"@echo off
setlocal
set ""PID={currentProcessId}""
set ""SOURCE={extractPath}""
set ""TARGET={appDirectory}""
set ""APP={appPath}""

:wait
tasklist /FI ""PID eq %PID%"" | find ""%PID%"" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto wait
)

xcopy ""%SOURCE%\*"" ""%TARGET%\"" /E /Y /I
start """" ""%APP%""
rmdir /S /Q {Quote(updateRoot)}
";

            File.WriteAllText(scriptPath, script);
            return scriptPath;
        }

        private static string Quote(string value) => "\"" + value + "\"";
    }

    public sealed class GitHubUpdate
    {
        public GitHubUpdate(Version version, string downloadUrl, string releaseUrl)
        {
            Version = version;
            DownloadUrl = downloadUrl;
            ReleaseUrl = releaseUrl;
        }

        public Version Version { get; }
        public string DownloadUrl { get; }
        public string ReleaseUrl { get; }
    }

    [DataContract]
    internal sealed class GitHubRelease
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; } = string.Empty;

        [DataMember(Name = "html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [DataMember(Name = "assets")]
        public GitHubReleaseAsset[] Assets { get; set; } = Array.Empty<GitHubReleaseAsset>();
    }

    [DataContract]
    internal sealed class GitHubReleaseAsset
    {
        [DataMember(Name = "name")]
        public string Name { get; set; } = string.Empty;

        [DataMember(Name = "browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
