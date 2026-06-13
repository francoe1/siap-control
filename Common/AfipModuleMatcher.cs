using SiapControl.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SiapControl.Common
{
    public sealed class AfipModuleMatch
    {
        public AfipApplicationCatalogItem CatalogItem { get; set; } = new AfipApplicationCatalogItem();
        public double Confidence { get; set; }
        public bool IsSafe { get; set; }
    }

    public sealed class AfipModuleMatcher
    {
        public AfipModuleMatch? FindSafeMatch(ModuleModel module, IEnumerable<AfipApplicationCatalogItem> catalog)
        {
            string moduleName = Normalize(module.AppName);
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                return null;
            }

            AfipModuleMatch[] candidates = catalog
                .Select(item => new AfipModuleMatch { CatalogItem = item, Confidence = Score(moduleName, item) })
                .Where(match => match.Confidence >= 0.72)
                .OrderByDescending(match => match.Confidence)
                .Take(2)
                .ToArray();

            if (candidates.Length == 0)
            {
                return null;
            }

            AfipModuleMatch best = candidates[0];
            best.IsSafe = best.Confidence >= 0.90 || candidates.Length == 1 || best.Confidence - candidates[1].Confidence >= 0.18;
            return best.IsSafe ? best : null;
        }

        public static string Normalize(string value)
        {
            string normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            string result = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            result = Regex.Replace(result, @"\b(version|versiones|release|vigente|periodo|periodos|aplicativo|impuesto)\b", " ");
            result = Regex.Replace(result, @"\d+", " ");
            result = Regex.Replace(result, @"[^a-z0-9]+", " ");
            return Regex.Replace(result, @"\s+", " ").Trim();
        }

        private static double Score(string moduleName, AfipApplicationCatalogItem item)
        {
            string title = Normalize(item.Title);
            string category = Normalize(item.Category);
            string combined = Normalize(item.Category + " " + item.Title + " " + item.Keywords);

            if (title.Equals(moduleName, StringComparison.OrdinalIgnoreCase) || category.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0;
            }

            if (ContainsWordSequence(title, moduleName) || ContainsWordSequence(category, moduleName))
            {
                return 0.95;
            }

            if (ContainsWordSequence(combined, moduleName))
            {
                return 0.88;
            }

            string[] moduleTokens = moduleName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] itemTokens = combined.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (moduleTokens.Length == 0 || itemTokens.Length == 0)
            {
                return 0;
            }

            int common = moduleTokens.Count(token => itemTokens.Contains(token));
            return (double)common / moduleTokens.Length;
        }

        private static bool ContainsWordSequence(string value, string sequence)
        {
            return (" " + value + " ").IndexOf(" " + sequence + " ", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
