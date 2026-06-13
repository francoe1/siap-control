using Microsoft.Win32;
using System;
using System.Reflection;

namespace SiapControl.Common
{
    public interface IWindowsStartupService
    {
        bool IsEnabled();
        void SetEnabled(bool enabled);
    }

    public sealed class WindowsStartupService : IWindowsStartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "SiapControl";

        public bool IsEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            string expected = Quote(Assembly.GetExecutingAssembly().Location);
            return key?.GetValue(ValueName) is string value &&
                   value.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        public void SetEnabled(bool enabled)
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled)
            {
                key.SetValue(ValueName, Quote(Assembly.GetExecutingAssembly().Location));
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }

        private static string Quote(string value) => "\"" + value + "\"";
    }
}
