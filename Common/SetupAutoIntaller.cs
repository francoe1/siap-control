using SiapControl.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SiapControl.Common
{
    public class SetupAutoInstaller
    {
        private readonly string _title;
        private readonly string _path;
        private readonly string[] _windows = new string[]
        {
            "Instalación de {0}",
            "{0} - Elegir grupo de programas",
        };

        private const string DIALOG_CLASS = "#32770 (Dialog)";
        private const string BUTTON_CLASS = "Button";
        private const string BUTTON1_CLASS = "button";
        private const string LABEL_CLASS = "Static";
        private const string GHOST_CLASS = "Ghost";
        private const string PROGRESS_BAR = "ThunderRT5PictureBox";

        public SetupAutoInstaller(string title, string path)
        {
            _title = title;
            _path = path;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const uint VK_RETURN = 0x0D;
        private const int BUFFER_SIZE = 256;

        public async Task<bool> InstallAsync()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(_path)
            {
                UseShellExecute = true
            };

            Process process = Process.Start(startInfo);
            bool successfull = false;

            if (process == null)
            {
                return false;
            }

            while (!FindWindow().Any())
            {
                await Task.Delay(100);
            }

            while (!process.HasExited)
            {
                await Task.Delay(100);
                foreach (var win in FindWindow())
                {
                    WindowManipulator.MakeWindowTransparent(win, 50);

                    if (win == IntPtr.Zero)
                    {
                        MessageBox.Show("La instalación no puede continuar de forma automatica");
                    }

                    string windowText = GetWindowContent(win);
                    successfull = GetAllChildLabels(win).Select(x => GetWindowContent(x)).Any(x => x.EndsWith("ha terminado correctamente.", StringComparison.InvariantCultureIgnoreCase));

                    foreach (var button in GetAllChildButtons(win))
                    {
                        string buttonText = GetWindowContent(button);

                        if (buttonText.Equals("Aceptar") || buttonText.Equals(string.Empty) || buttonText.Equals("&Continuar") || (successfull && buttonText.Equals("OK")))
                        {
                            await PressEnter(button);
                        }
                    }
                }
            }

            if (!successfull)
            {
                foreach (var win in FindWindow())
                {
                    WindowManipulator.MakeWindowTransparent(win, 255);
                }
            }

            return successfull;
        }

        private static async Task PressEnter(IntPtr button)
        {
            PostMessage(button, WM_KEYDOWN, (IntPtr)VK_RETURN, IntPtr.Zero);
            await Task.Delay(10);
            PostMessage(button, WM_KEYUP, (IntPtr)VK_RETURN, IntPtr.Zero);
        }

        private IEnumerable<IntPtr> FindWindow()
        {
            foreach (var window in _windows)
            {
                var title = string.Format(window, _title);
                IntPtr hWnd = FindWindow(null, title);
                if (hWnd != IntPtr.Zero)
                {
                    yield return hWnd;
                }
            }
        }

        private string GetWindowContent(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(BUFFER_SIZE);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private IEnumerable<IntPtr> GetAllChildButtons(IntPtr hWnd)
        {
            IntPtr child = IntPtr.Zero;
            List<IntPtr> buttons = new List<IntPtr>();

            while ((child = FindWindowEx(hWnd, child, "ThunderRT5CommandButton", null)) != IntPtr.Zero)
            {
                buttons.Add(child);
            }

            while ((child = FindWindowEx(hWnd, child, "Button", null)) != IntPtr.Zero)
            {
                buttons.Add(child);
            }

            return buttons;
        }

        private IEnumerable<IntPtr> GetAllChildLabels(IntPtr hWnd)
        {
            IntPtr child = IntPtr.Zero;
            List<IntPtr> buttons = new List<IntPtr>();

            while ((child = FindWindowEx(hWnd, child, "Static", null)) != IntPtr.Zero)
            {
                buttons.Add(child);
            }

            return buttons;
        }
    }
}