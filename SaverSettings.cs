using Microsoft.Win32;
using System;

namespace MatrixSCR
{
    internal sealed class SaverSettings
    {
        private const string RegistryPath = @"Software\MatrixSCR";

        public double Speed = 1.0;
        public int Density = 20;
        public int Fps = 33;
        public bool Fog = true;
        public bool Waves = true;
        public bool Rotate = true;
        public string Mode = "Matrix";

        public static SaverSettings Load()
        {
            SaverSettings result = new SaverSettings();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key == null) return result;
                    result.Speed = Clamp(ReadDouble(key, "Speed", result.Speed), 0.25, 3.0);
                    result.Density = Clamp(ReadInt(key, "Density", result.Density), 1, 100);
                    result.Fps = Clamp(ReadInt(key, "Fps", result.Fps), 15, 60);
                    result.Fog = ReadInt(key, "Fog", 1) != 0;
                    result.Waves = ReadInt(key, "Waves", 1) != 0;
                    result.Rotate = ReadInt(key, "Rotate", 1) != 0;
                    string mode = Convert.ToString(key.GetValue("Mode", result.Mode));
                    if (mode == "Matrix" || mode == "Binary" || mode == "Hexadecimal" ||
                        mode == "Decimal" || mode == "DNA") result.Mode = mode;
                }
            }
            catch { }
            return result;
        }

        public void Save()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
            {
                key.SetValue("Speed", Speed.ToString(System.Globalization.CultureInfo.InvariantCulture));
                key.SetValue("Density", Density, RegistryValueKind.DWord);
                key.SetValue("Fps", Fps, RegistryValueKind.DWord);
                key.SetValue("Fog", Fog ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("Waves", Waves ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("Rotate", Rotate ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("Mode", Mode, RegistryValueKind.String);
            }
        }

        private static int ReadInt(RegistryKey key, string name, int fallback)
        {
            try { return Convert.ToInt32(key.GetValue(name, fallback)); }
            catch { return fallback; }
        }

        private static double ReadDouble(RegistryKey key, string name, double fallback)
        {
            double value;
            return double.TryParse(Convert.ToString(key.GetValue(name, fallback)),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private static int Clamp(int value, int min, int max)
        { return Math.Max(min, Math.Min(max, value)); }

        private static double Clamp(double value, double min, double max)
        { return Math.Max(min, Math.Min(max, value)); }
    }
}
