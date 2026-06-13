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
            string[] moduleNames = GetModuleSearchNames(module)
                .Select(Normalize)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (moduleNames.Length == 0)
            {
                return null;
            }

            AfipModuleMatch[] candidates = catalog
                .Select(item => new AfipModuleMatch { CatalogItem = item, Confidence = moduleNames.Max(moduleName => Score(moduleName, item)) })
                .Where(match => match.Confidence >= 0.88)
                .OrderByDescending(match => match.Confidence)
                .Take(2)
                .ToArray();

            if (candidates.Length == 0)
            {
                return null;
            }

            AfipModuleMatch best = candidates[0];
            best.IsSafe = best.Confidence >= 0.95 || (best.Confidence >= 0.90 && (candidates.Length == 1 || best.Confidence - candidates[1].Confidence >= 0.18));
            return best.IsSafe ? best : null;
        }

        public static IEnumerable<string> GetModuleSearchNames(ModuleModel module)
        {
            yield return module.AppName;
            yield return module.IconName;
            yield return module.ProductName;
            yield return module.FileDescription;
            yield return module.InternalName;
            yield return module.OriginalFilename;
            yield return module.ExecutableName;
            yield return module.CompanyName;
            yield return module.Comments;
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

            if (title.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                return 1.0;
            }

            if (ContainsWordSequence(combined, moduleName))
            {
                string[] moduleSequenceTokens = moduleName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] titleSequenceTokens = title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (titleSequenceTokens.Length > 0 && (double)moduleSequenceTokens.Length / titleSequenceTokens.Length >= 0.80)
                {
                    return 0.95;
                }
            }

            string[] moduleTokens = moduleName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] itemTokens = combined.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (moduleTokens.Length == 0 || itemTokens.Length == 0)
            {
                return 0;
            }

            int common = moduleTokens.Count(token => itemTokens.Contains(token));
            int titleCommon = moduleTokens.Count(token => title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Contains(token));
            double moduleCoverage = (double)common / moduleTokens.Length;
            double titleCoverage = title.Length == 0 ? 0 : (double)titleCommon / title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            double coverage = Math.Min(moduleCoverage, titleCoverage);
            return moduleTokens.Length >= 2 && coverage >= 0.80 ? coverage : 0;
        }

        private static bool ContainsWordSequence(string value, string sequence)
        {
            return (" " + value + " ").IndexOf(" " + sequence + " ", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
