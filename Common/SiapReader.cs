using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);
                return new ModuleModel
                {
                    AppName = info.ProductName,
                    AppVersion = info.ProductVersion
                };
            }
            catch
            {
                return new ModuleModel { AppName = Path.GetFileNameWithoutExtension(file), AppVersion = "undefined" };
            }
        }
    }
}
