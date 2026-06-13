using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SiapControl.Common
{
    public class SetupReader
    {
        public string AppName { get; set; }
        public string AppVersion { get; set; }

        private string _path { get; set; }
        private string _pathLST { get; set; }
        private string[] _params { get; set; }

        public string Path => _path;

        public SetupReader(string path)
        {
            _path = path;
            _pathLST = $"{_path.ToLower().Replace(".exe", ".lst")}";
        }


        public bool Open()
        {
            try
            {
                if (!System.IO.Path.GetExtension(_path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("El archivo debe ser un ejecutable", "Error");
                    return false;
                }

                if (!File.Exists(_path))
                {
                    MessageBox.Show("No se encuentra el archivo", "Error");
                    return false;
                }

                if (!File.Exists(_pathLST))
                {
                    MessageBox.Show("No se encuentra el archivo .LST", "Error");
                    return false;
                }

                _params = File.ReadAllLines(_pathLST);

                Parameter pName = Parameters.Where(x => x.Name.Equals("AppExe")).SingleOrDefault();
                Parameter pVersion = Parameters.Where(x => x.Name.Equals("VersionApp")).SingleOrDefault();

                if (pName.IsValid) AppName = pName.Value.Replace(".exe", "");
                if (pVersion.IsValid) AppVersion = pVersion.Value.Replace("\"", "");
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message + "\n¿Deseas continuar con la instalación?", "Error") == DialogResult.Cancel) return false;
            }
            return true;
        }

        public async Task CreateBackupAsync(string userPath) => await Task.Factory.StartNew(() => CreateBackup(userPath));

        public void CreateBackup(string userPath)
        {
            string path = userPath + @"\_backup";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            string appPath = $@"{userPath}\{AppName}";
            if (!Directory.Exists(appPath)) return;

            string zipPath = $@"{path}\{AppName}.zip";
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(appPath, zipPath);

        }

        public IEnumerable<Parameter> Parameters
        {
            get
            {
                {
                    for (int i = 0; i < _params.Length; i++)
                    {
                        string[] parts = _params[i].Split('=');
                        if (parts.Length < 2) continue;

                        string name = parts[0].TrimStart('=');
                        string value = _params[i].Replace(parts[0], "").TrimStart('=');

                        yield return new Parameter(name, value);
                    }
                }
            }
        }

        public readonly struct Parameter
        {
            public string Name { get; }
            public string Value { get; }

            public bool IsValid => !string.IsNullOrEmpty(Name);

            public Parameter(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}
