using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SiapControl.Common
{
    public sealed class SiapInstallation
    {
        public SiapInstallation(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; }
        public string Path { get; }
    }

    public static class SiapInstallationFinder
    {
        private const string SiapExecutableName = "siap.exe";
        private const string AfipDatabaseName = "AFIP.MDB";

        public static IReadOnlyList<SiapInstallation> FindAll(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return Array.Empty<SiapInstallation>();
            }

            return EnumerateDirectories(rootPath)
                .Where(IsSiapInstallation)
                .Select(path => new SiapInstallation(GetInstallationName(path), path))
                .OrderBy(installation => installation.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> EnumerateDirectories(string rootPath)
        {
            var pending = new Stack<string>();
            pending.Push(rootPath);

            while (pending.Count > 0)
            {
                string current = pending.Pop();
                yield return current;

                IEnumerable<string> children;
                try
                {
                    children = Directory.EnumerateDirectories(current).ToList();
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (string child in children)
                {
                    pending.Push(child);
                }
            }
        }

        private static bool IsSiapInstallation(string path)
        {
            return ContainsFile(path, SiapExecutableName) && ContainsFile(path, AfipDatabaseName);
        }

        private static bool ContainsFile(string path, string fileName)
        {
            try
            {
                return Directory.EnumerateFiles(path)
                    .Any(file => string.Equals(System.IO.Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase));
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static string GetInstallationName(string path)
        {
            string trimmed = path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            string name = System.IO.Path.GetFileName(trimmed);
            return string.IsNullOrWhiteSpace(name) ? path : name;
        }
    }
}
