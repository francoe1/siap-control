using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SiapControl.Common
{
    public static class AfipVersionComparer
    {
        public static bool TryCompare(string installedVersion, string remoteVersion, out int comparison)
        {
            int[] installed = ParseNumbers(installedVersion);
            int[] remote = ParseNumbers(remoteVersion);
            comparison = 0;

            if (installed.Length == 0 || remote.Length == 0)
            {
                return false;
            }

            int length = Math.Max(installed.Length, remote.Length);
            for (int i = 0; i < length; i++)
            {
                int left = i < installed.Length ? installed[i] : 0;
                int right = i < remote.Length ? remote[i] : 0;
                if (left == right)
                {
                    continue;
                }

                comparison = left.CompareTo(right);
                return true;
            }

            return true;
        }

        public static bool IsRemoteNewer(string installedVersion, string remoteVersion)
        {
            return TryCompare(installedVersion, remoteVersion, out int comparison) && comparison < 0;
        }

        public static string ExtractVersionText(string value)
        {
            Match match = Regex.Match(value, @"(?:Versi[oó]n\s*)?(?<version>\d+(?:[\.\s]\d+)*(?:\s*(?:Release|R)\s*\d+)?)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["version"].Value.Trim() : string.Empty;
        }

        public static string RemoveVersionText(string value)
        {
            string result = Regex.Replace(value ?? string.Empty, @"\bVersi\S*n\s+\d+(?:[\.\s]\d+)*(?:\s*(?:Release|R)\s*\d+)?", " ", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\b\d+(?:[\.\s]\d+)*(?:\s*(?:Release|R)\s*\d+)\b", " ", RegexOptions.IgnoreCase);
            return Regex.Replace(result, @"\s+", " ").Trim();
        }

        private static int[] ParseNumbers(string value)
        {
            return Regex.Matches(value ?? string.Empty, @"\d+")
                .Cast<Match>()
                .Select(match => int.Parse(match.Value))
                .ToArray();
        }
    }
}
