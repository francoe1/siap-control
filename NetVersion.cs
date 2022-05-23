using Microsoft.Win32;

namespace SiapControl
{
    public class NetVersion
    {
        public string Version { get; }
        public int VersionN { get; private set; }

        public NetVersion()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    Version = $".NET Framework Version: {CheckFor45PlusVersion((int)ndpKey.GetValue("Release"))}";
                }
                else
                {
                    Version = ".NET Framework Version 4.5 or later is not detected.";
                }
            }
        }

        private string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 528040)
            {
                VersionN = 480;
                return "4.8 or later";
            }
            if (releaseKey >= 461808)
            {
                VersionN = 472;
                return "4.7.2";
            }
            if (releaseKey >= 461308)
            {
                VersionN = 471;
                return "4.7.1";
            }
            if (releaseKey >= 460798)
            {
                VersionN = 470;
                return "4.7";
            }
            if (releaseKey >= 394802)
            {
                VersionN = 462;
                return "4.6.2";
            }
            if (releaseKey >= 394254)
            {
                VersionN = 461;
                return "4.6.1";
            }
            if (releaseKey >= 393295)
            {
                VersionN = 460;
                return "4.6";
            }
            if (releaseKey >= 379893)
            {
                VersionN = 452;
                return "4.5.2";
            }
            if (releaseKey >= 378675)
            {
                VersionN = 451;
                return "4.5.1";
            }
            if (releaseKey >= 378389)
            {
                VersionN = 450;
                return "4.5";
            }
            VersionN = 0;
            return "No 4.5 or later version detected";
        }
    }
}