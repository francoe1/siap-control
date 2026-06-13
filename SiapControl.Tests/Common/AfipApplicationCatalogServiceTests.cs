using System;
using System.Linq;
using SiapControl.Common;
using Xunit;

namespace SiapControl.Tests.Common
{
    public class AfipApplicationCatalogServiceTests
    {
        [Fact]
        public void ParseCatalog_ReadsItems()
        {
            const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<tabla>
  <item>
    <categoria>IVA</categoria>
    <title>Version IVA</title>
    <link>/aplicativos/IVA/default.asp</link>
    <clave>impuesto, valor agregado</clave>
  </item>
</tabla>";

            AfipApplicationCatalogItem item = AfipApplicationCatalogService.ParseCatalog(xml).Single();

            Assert.Equal("IVA", item.Category);
            Assert.Equal("Version IVA", item.Title);
            Assert.Equal("/aplicativos/IVA/default.asp", item.Link);
        }

        [Fact]
        public void ParsePackageDetail_ExtractsVersionDateAndApplicationZip()
        {
            var catalogItem = new AfipApplicationCatalogItem { Title = "Version IVA", Link = "/aplicativos/IVA/default.asp" };
            var detailUri = new Uri("https://www.afip.gob.ar/aplicativos/IVA/default.asp");
            const string html = @"<h2>IVA</h2>
<h5>VERSION: <strong>6.0 Release 3</strong></h5>
<a href=""/Aplicativos/IVA/archivos/iva.zip""><li>Aplicativo</li></a>
<a href=""/aplicativos/siap/archivos/siap.zip""><li>Aplicativo SIAp</li></a>
<li><strong>Fecha de publicacion:</strong> 02/09/2025</li>";

            AfipApplicationPackage package = AfipApplicationCatalogService.ParsePackageDetail(catalogItem, detailUri, html)!;

            Assert.Equal("IVA", package.DisplayName);
            Assert.Equal("6.0 Release 3", package.VersionText);
            Assert.Equal(new DateTime(2025, 9, 2), package.PublishedAt);
            Assert.Equal("https://www.afip.gob.ar/Aplicativos/IVA/archivos/iva.zip", package.DownloadUri.ToString());
        }
    }
}
