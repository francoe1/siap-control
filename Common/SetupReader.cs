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
        private string m_path { get; set; }
        private string m_pathLST { get; set; }
        public string AppName { get; set; }
        public string AppVersion { get; set; }

        private string[] m_params { get; set; }

        private string _lstContent { get; set; }

        public SetupReader(string path)
        {
            m_path = path;
            m_pathLST = $"{m_path.ToLower().Replace(".exe", ".lst")}";
        }

        public bool Open()
        {
            try
            {
                if (Path.GetExtension(m_path).ToLower().Equals("exe"))
                {
                    MessageBox.Show("El archivo debe ser un ejecutable", "Error");
                    return false;
                }

                if (!File.Exists(m_path))
                {
                    MessageBox.Show("No se encuentra el archivo", "Error");
                    return false;
                }

                if (!File.Exists(m_pathLST))
                {
                    MessageBox.Show("No se encuentra el archivo .LST", "Error");
                    return false;
                }

                m_params = File.ReadAllLines(m_pathLST);
                _lstContent = File.ReadAllText(m_pathLST);

                Parameter pName = Parameters.Where(x => x.Name.Equals("AppExe")).SingleOrDefault();
                Parameter pVersion = Parameters.Where(x => x.Name.Equals("VersionApp")).SingleOrDefault();

                if (pName is object) AppName = pName.Value.Replace(".exe", "");
                if (pVersion is object) AppVersion = pVersion.Value.Replace("\"", "");
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message + "\n¿Deseas continuar con la instalación?", "Error") == DialogResult.Cancel) return false;
            }
            return true;
        }

        public async Task<bool> RunInstallerAsync(string targetPath) => await Task.Factory.StartNew(() => RunInstaller(targetPath));

        private bool RunInstaller(string targetPath)
        {
            Process process = new Process();
            process.StartInfo.FileName = m_path;
            process.StartInfo.CreateNoWindow = true;
            // process.StartInfo.Arguments = "/quiet";
            process.StartInfo.Verb = "runas";

            File.WriteAllText(m_pathLST, Regex.Replace(_lstContent, @"DefaultDir=\$\(ProgramFiles\)\\", "DefaultDir=" + targetPath.Replace("\\", "\\\\") + "\\"));

            if (!process.Start())
            {
                MessageBox.Show("Error al iniciar el instalador");
                return false;
            }

            process.WaitForExit();

            MessageBox.Show(process.ExitCode.ToString());

            return MessageBox.Show(
                "¿La instalación se realizo correctamente?",
                "Finalizo la instalación",
                MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        public async Task CreateBackupAsync(string userPath) => await Task.Factory.StartNew(() => CreateBackup(userPath));

        public void CreateBackup(string userPath)
        {
            try
            {
                string path = userPath + @"\_backup";
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string appPath = $@"{userPath}\{AppName}";
                if (!Directory.Exists(appPath)) return;

                string zipPath = $@"{path}\{AppName}.zip";
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(appPath, zipPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al crear backup");
            }
        }

        public void Close()
        {
            File.WriteAllText(m_pathLST, _lstContent);
        }

        private IEnumerable<Parameter> Parameters
        {
            get
            {
                {
                    for (int i = 0; i < m_params.Length; i++)
                    {
                        string[] parts = m_params[i].Split('=');
                        if (parts.Length < 2) continue;

                        string name = parts[0].TrimStart('=');
                        string value = m_params[i].Replace(parts[0], "").TrimStart('=');

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