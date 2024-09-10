using System;
using System.Runtime.InteropServices;

namespace SiapControl.Utils
{
    public static class WindowManipulator
    {
        // Importar la función IsWindow para verificar si el handle es válido
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        // Constantes para ShowWindow
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        /// <summary>
        /// Oculta la ventana especificada por su handle.
        /// </summary>
        /// <param name="hWnd">Handle de la ventana a ocultar.</param>
        /// <returns>True si la operación fue exitosa, de lo contrario false.</returns>
        public static bool HideWindow(IntPtr hWnd)
        {
            if (IsWindow(hWnd))
            {
                return ShowWindow(hWnd, SW_HIDE);
            }
            else
            {
                Console.WriteLine("Handle de ventana inválido.");
                return false;
            }
        }

        /// <summary>
        /// Muestra la ventana especificada por su handle.
        /// </summary>
        /// <param name="hWnd">Handle de la ventana a mostrar.</param>
        /// <returns>True si la operación fue exitosa, de lo contrario false.</returns>
        public static bool ShowWindow(IntPtr hWnd)
        {
            if (IsWindow(hWnd))
            {
                return ShowWindow(hWnd, SW_SHOW);
            }
            else
            {
                Console.WriteLine("Handle de ventana inválido.");
                return false;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int GWL_EXSTYLE = -20;           // Obtener/establecer estilos extendidos
        private const uint WS_EX_LAYERED = 0x80000;    // Estilo para ventanas en capas
        private const uint LWA_ALPHA = 0x2;            // Usar valor alfa para la transparencia

        private const int SW_SHOWNOACTIVATE = 4;       // Mostrar ventana sin activarla

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Establece la transparencia de la ventana
        public static void MakeWindowTransparent(IntPtr hWnd, byte transparency)
        {
            uint currentStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, currentStyle | WS_EX_LAYERED);

            // Establecer transparencia
            SetLayeredWindowAttributes(hWnd, 0, transparency, LWA_ALPHA);

            // Intentar mostrar la ventana para aplicar cambios
            ShowWindow(hWnd, SW_SHOWNOACTIVATE);
        }
    }
}