using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SiapControl.Common
{
    public sealed class AfipApplicationCatalogItem
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
    }

    public sealed class AfipApplicationPackage
    {
        public AfipApplicationCatalogItem CatalogItem { get; set; } = new AfipApplicationCatalogItem();
        public string DisplayName { get; set; } = string.Empty;
        public string VersionText { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public Uri DownloadUri { get; set; } = null!;
        public Uri DetailUri { get; set; } = null!;
    }

    public interface IAfipApplicationCatalogService
    {
        Task<IReadOnlyList<AfipApplicationCatalogItem>> GetCatalogAsync();
        Task<AfipApplicationPackage?> ResolvePackageAsync(AfipApplicationCatalogItem item);
    }

    public sealed class AfipApplicationCatalogService : IAfipApplicationCatalogService
    {
        private static readonly Uri BaseUri = new Uri("https://www.afip.gob.ar");
        private static readonly Uri CatalogUri = new Uri(BaseUri, "/aplicativos/xml/tabla.xml");
        private static readonly TimeSpan CatalogCacheDuration = TimeSpan.FromHours(1);
        private static readonly object CatalogCacheLock = new object();
        private static IReadOnlyList<AfipApplicationCatalogItem>? _cachedCatalog;
        private static DateTime _cachedCatalogAt;
        private readonly HttpClient _client;

        public AfipApplicationCatalogService()
            : this(CreateClient())
        {
        }

        public AfipApplicationCatalogService(HttpClient client)
        {
            _client = client;
        }

        public async Task<IReadOnlyList<AfipApplicationCatalogItem>> GetCatalogAsync()
        {
            lock (CatalogCacheLock)
            {
                if (_cachedCatalog != null && DateTime.Now - _cachedCatalogAt < CatalogCacheDuration)
                {
                    return _cachedCatalog;
                }
            }

            string xml = await _client.GetStringAsync(CatalogUri);
            IReadOnlyList<AfipApplicationCatalogItem> catalog = ParseCatalog(xml);
            lock (CatalogCacheLock)
            {
                _cachedCatalog = catalog;
                _cachedCatalogAt = DateTime.Now;
            }

            return catalog;
        }

        public async Task<AfipApplicationPackage?> ResolvePackageAsync(AfipApplicationCatalogItem item)
        {
            Uri itemUri = ResolveUri(item.Link, BaseUri);
            if (IsZip(itemUri))
            {
                return new AfipApplicationPackage
                {
                    CatalogItem = item,
                    DisplayName = item.Title,
                    VersionText = AfipVersionComparer.ExtractVersionText(item.Title),
                    DetailUri = itemUri,
                    DownloadUri = itemUri
                };
            }

            string html = await _client.GetStringAsync(itemUri);
            return ParsePackageDetail(item, itemUri, html);
        }

        public static IReadOnlyList<AfipApplicationCatalogItem> ParseCatalog(string xml)
        {
            XDocument doc = XDocument.Parse(RemoveUtf8Bom(xml));
            return doc.Descendants("item")
                .Select(item => new AfipApplicationCatalogItem
                {
                    Category = ReadElement(item, "categoria"),
                    Title = ReadElement(item, "title"),
                    Link = ReadElement(item, "link"),
                    Keywords = ReadElement(item, "clave")
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Link))
                .ToArray();
        }

        public static AfipApplicationPackage? ParsePackageDetail(AfipApplicationCatalogItem item, Uri detailUri, string html)
        {
            string decoded = WebUtility.HtmlDecode(html);
            string displayName = ExtractHeading(decoded);
            string version = ExtractVersion(decoded);
            DateTime? publishedAt = ExtractPublicationDate(decoded);
            Uri? downloadUri = ExtractApplicationZipUri(detailUri, decoded);

            if (downloadUri == null)
            {
                return null;
            }

            return new AfipApplicationPackage
            {
                CatalogItem = item,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? item.Title : displayName,
                VersionText = string.IsNullOrWhiteSpace(version) ? AfipVersionComparer.ExtractVersionText(item.Title) : version,
                PublishedAt = publishedAt,
                DetailUri = detailUri,
                DownloadUri = downloadUri
            };
        }

        private static string ReadElement(XElement item, string name)
        {
            return (item.Element(name)?.Value ?? string.Empty).Trim();
        }

        private static Uri ResolveUri(string value, Uri relativeTo)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out Uri absolute)
                ? absolute
                : new Uri(relativeTo, value);
        }

        private static bool IsZip(Uri uri)
        {
            return uri.AbsolutePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractHeading(string html)
        {
            Match match = Regex.Match(html, @"<h2[^>]*>(?<value>.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? CleanHtml(match.Groups["value"].Value) : string.Empty;
        }

        private static string ExtractVersion(string html)
        {
            Match match = Regex.Match(html, @"VERSI\S*N\s*:\s*(?:<[^>]+>|\s)*(?<value>[^<\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? Regex.Replace(CleanHtml(match.Groups["value"].Value), "VIGENTE", "", RegexOptions.IgnoreCase).Trim(' ', '-') : string.Empty;
        }

        private static DateTime? ExtractPublicationDate(string html)
        {
            Match match = Regex.Match(html, @"Fecha de publicaci\S*n\s*:\s*(?:<[^>]+>|\s)*(?<date>\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            return DateTime.TryParseExact(match.Groups["date"].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
                ? date.Date
                : (DateTime?)null;
        }

        private static Uri? ExtractApplicationZipUri(Uri detailUri, string html)
        {
            foreach (Match match in Regex.Matches(html, @"<a\s+[^>]*href\s*=\s*[""'](?<href>[^""']+\.zip)[""'][^>]*>(?<body>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                string body = CleanHtml(match.Groups["body"].Value);
                if (body.IndexOf("Aplicativo", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (body.IndexOf("SIAp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                return ResolveUri(match.Groups["href"].Value, detailUri);
            }

            Match fallback = Regex.Match(html, @"href\s*=\s*[""'](?<href>[^""']+\.zip)[""']", RegexOptions.IgnoreCase);
            return fallback.Success ? ResolveUri(fallback.Groups["href"].Value, detailUri) : null;
        }

        private static string CleanHtml(string value)
        {
            string withoutTags = Regex.Replace(value, "<.*?>", " ");
            return Regex.Replace(WebUtility.HtmlDecode(withoutTags), @"\s+", " ").Trim();
        }

        private static string RemoveUtf8Bom(string value)
        {
            return value.TrimStart('\uFEFF', 'ï', '»', '¿');
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
