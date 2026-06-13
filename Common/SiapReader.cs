using SiapControl.Data.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SiapControl.Common
{
    public class SiapReader
    {
        public ModuleModel[] Modules { get; private set; }

        public bool HasError { get; private set; } = false;

        public SiapReader(string path)
        {
            List<string> files = new List<string>();
            HasError = true;
            if (File.Exists(path + "/siap.exe"))
            {
                HasError = false;
                foreach (string dir in Directory.GetDirectories(path))
                {
                    string dir_name = dir.Split(Path.DirectorySeparatorChar).ToList().Last().ToLower();
                    foreach (string file in Directory.GetFiles(dir, "*.exe"))
                    {
                        if (dir_name.Equals(Path.GetFileNameWithoutExtension(file).ToLower()))
                        {
                            files.Add(file);
                        }
                    }
                }

                Modules = new ModuleModel[files.Count];

                for (int i = 0; i < files.Count; i++)
                {
                    Modules[i] = GetModuleModel(files[i]);
                }
            }
        }

        public static ModuleModel GetModuleModel(string file)
        {
            string executableName = Path.GetFileNameWithoutExtension(file);
            string iconName = GetIconName(file);
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);
                string productName = info.ProductName ?? string.Empty;
                string productVersion = info.ProductVersion ?? string.Empty;
                return new ModuleModel
                {
                    AppName = FirstValue(iconName, productName, executableName),
                    AppVersion = string.IsNullOrWhiteSpace(productVersion) ? "undefined" : productVersion,
                    ExecutableName = executableName,
                    IconName = iconName,
                    ProductName = productName,
                    FileDescription = info.FileDescription ?? string.Empty,
                    InternalName = info.InternalName ?? string.Empty,
                    OriginalFilename = info.OriginalFilename ?? string.Empty,
                    CompanyName = info.CompanyName ?? string.Empty,
                    Comments = info.Comments ?? string.Empty,
                    FileVersion = info.FileVersion ?? string.Empty
                };
            }
            catch
            {
                return new ModuleModel
                {
                    AppName = FirstValue(iconName, executableName),
                    AppVersion = "undefined",
                    ExecutableName = executableName,
                    IconName = iconName
                };
            }
        }

        private static string GetIconName(string executablePath)
        {
            string? directory = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return string.Empty;
            }

            string? icon = Directory.GetFiles(directory, "*.ico")
                .OrderBy(Path.GetFileName)
                .FirstOrDefault();

            return icon == null ? string.Empty : Path.GetFileNameWithoutExtension(icon);
        }

        private static string FirstValue(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }
    }
}
