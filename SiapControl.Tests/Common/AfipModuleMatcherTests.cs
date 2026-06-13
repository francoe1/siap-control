using System.Collections.Generic;
using SiapControl.Common;
using SiapControl.Data.Models;
using Xunit;

namespace SiapControl.Tests.Common
{
    public class AfipModuleMatcherTests
    {
        [Fact]
        public void FindSafeMatch_MatchesNormalizedNames()
        {
            var matcher = new AfipModuleMatcher();
            var module = new ModuleModel { AppName = "IVA", AppVersion = "6.0 Release 2" };
            var catalog = new List<AfipApplicationCatalogItem>
            {
                new AfipApplicationCatalogItem { Category = "IVA", Title = "Version IVA", Link = "/iva" },
                new AfipApplicationCatalogItem { Category = "Seguridad Social", Title = "Version SICOSS", Link = "/sicoss" }
            };

            AfipModuleMatch match = matcher.FindSafeMatch(module, catalog);

            Assert.NotNull(match);
            Assert.Equal("/iva", match!.CatalogItem.Link);
            Assert.True(match.IsSafe);
        }

        [Fact]
        public void FindSafeMatch_UsesExecutableMetadataWhenAppNameDoesNotMatch()
        {
            var matcher = new AfipModuleMatcher();
            var module = new ModuleModel
            {
                AppName = "Modulo generico",
                ProductName = "IVA",
                OriginalFilename = "iva.exe"
            };
            var catalog = new List<AfipApplicationCatalogItem>
            {
                new AfipApplicationCatalogItem { Category = "IVA", Title = "Version IVA", Link = "/iva", Keywords = "valor agregado" }
            };

            AfipModuleMatch match = matcher.FindSafeMatch(module, catalog);

            Assert.NotNull(match);
            Assert.Equal("/iva", match.CatalogItem.Link);
        }

        [Fact]
        public void FindSafeMatch_DoesNotMatchSingleSharedGenericToken()
        {
            var matcher = new AfipModuleMatcher();
            var module = new ModuleModel { AppName = "Bienes Personales", AppVersion = "7.0" };
            var catalog = new List<AfipApplicationCatalogItem>
            {
                new AfipApplicationCatalogItem { Category = "Bienes Personales", Title = "Acciones y Participaciones Societarias", Link = "/acciones" },
                new AfipApplicationCatalogItem { Category = "Ganancia Personas Fisicas", Title = "Ganancias Personas Fisicas y Bienes Personales", Link = "/ganancias" }
            };

            AfipModuleMatch match = matcher.FindSafeMatch(module, catalog);

            Assert.Null(match);
        }

        [Fact]
        public void Normalize_RemovesAccentsAndVersionWords()
        {
            Assert.Equal("ganancia minima presunta", AfipModuleMatcher.Normalize("Ganancia Minima Presunta Version 9.0 Release 2"));
        }
    }
}
