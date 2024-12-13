using Microsoft.Win32;
using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;

namespace PcAnalyzer.Models
{
    public class OS : IExportable
    {
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public string Version { get; set; }
        public string Architecture { get; set; }
        public string LicenceType { get; set; }
        public string SerialNumber { get; set; }

        public string FormatData()
        {
            return GetOperatingSystemInfo();
        }
        public string GetName()
        {
            return "Операционная система";
        }
        static string GetOperatingSystemInfo()
        {
            return $"Версия: {GetOSVersion()}\n" +
                $"Архитектура: {RuntimeInformation.OSArchitecture.ToString()}\n" +
                $"Тип лицензии: {GetLicenceType()}\n" +
                $"Product Keys:\n{GetAllWindowsKeys()}";
        }

        static string GetOSVersion()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string osName = obj["Caption"]?.ToString() ?? "Unknown";
                        string osVersion = obj["Version"]?.ToString() ?? "Unknown";
                        return $"{osName} (Version: {osVersion})";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
            return "Unknown";
        }

        static string GetLicenceType()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var licenseStatus = Convert.ToInt32(obj["LicenseStatus"]);
                        return licenseStatus switch
                        {
                            1 => "Licensed",
                            2 => "Unlicensed",
                            3 => "Out-of-Box Grace",
                            4 => "Out-of-Tolerance Grace",
                            5 => "Non-Genuine Grace",
                            6 => "Notification",
                            _ => "Unknown"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
            return "Unknown";
        }
        static string GetAllWindowsKeys()
        {
            var foundKeys = new List<string>();

            // OEM ключ
            var oemKey = GetBackupProductKeyDefault();
            if (!string.IsNullOrEmpty(oemKey))
                foundKeys.Add("OEM ключ (из SoftwareProtectionPlatform): " + oemKey);

            // Ключ из DigitalProductId
            var classicKey = DecodeFromRegValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DigitalProductId");
            if (!string.IsNullOrEmpty(classicKey))
                foundKeys.Add("Ключ из DigitalProductId: " + classicKey);

            // Ключ из DigitalProductId4
            var dpId4Key = DecodeFromRegValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DigitalProductId4");
            if (!string.IsNullOrEmpty(dpId4Key))
                foundKeys.Add("Ключ из DigitalProductId4: " + dpId4Key);

            // Ключ из WMI (частичный или универсальный)
            var wmiPartialKey = GetPartialProductKeyFromWMI();
            if (!string.IsNullOrEmpty(wmiPartialKey))
                foundKeys.Add("Ключ из WMI: " + wmiPartialKey);

            // Проверяем SoftwareProtectionPlatform и подветки
            EnumerateSPPKeys(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", foundKeys);

            // Проверяем дополнительные места (старые версии Windows)
            CheckAdditionalLocations(foundKeys);

            if (foundKeys.Count > 0)
            {
                return string.Join(Environment.NewLine, foundKeys);
            }
            else
            {
                return "No keys found.";
            }
        }

        static string GetBackupProductKeyDefault()
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"))
            {
                if (subKey != null)
                {
                    var val = subKey.GetValue("BackupProductKeyDefault") as string;
                    if (!string.IsNullOrEmpty(val))
                    {
                        return val;
                    }
                }
            }
            return null;
        }

        static string DecodeFromRegValue(string subKeyPath, string valueName)
        {
            var key = DecodeFromRegValueInternal(subKeyPath, valueName, RegistryView.Registry64);
            if (!string.IsNullOrEmpty(key))
                return key;

            key = DecodeFromRegValueInternal(subKeyPath, valueName, RegistryView.Registry32);
            return key;
        }

        static string DecodeFromRegValueInternal(string subKeyPath, string valueName, RegistryView view)
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
            using (var subKey = baseKey.OpenSubKey(subKeyPath, false))
            {
                if (subKey == null) return null;
                var productIdData = subKey.GetValue(valueName) as byte[];
                if (productIdData == null) return null;
                return DecodeKey(productIdData);
            }
        }

        static string DecodeKey(byte[] digitalProductId)
        {
            const string keyChars = "BCDFGHJKMPQRTVWXY2346789";
            if (digitalProductId.Length < 0x34 + 15) return null;

            byte[] key = new byte[15];
            Array.Copy(digitalProductId, 0x34, key, 0, 15);

            char[] result = new char[29]; // 25 символов ключа + 4 дефиса
            int current;
            for (int i = 28; i >= 0; i--)
            {
                if ((i + 1) % 6 == 0)
                {
                    result[i] = '-';
                }
                else
                {
                    current = 0;
                    for (int j = 14; j >= 0; j--)
                    {
                        current = current * 256 ^ key[j];
                        key[j] = (byte)(current / 24);
                        current %= 24;
                    }
                    result[i] = keyChars[current];
                }
            }

            return new string(result);
        }

        static string GetPartialProductKeyFromWMI()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string partialKey = obj["PartialProductKey"] as string;
                        if (!string.IsNullOrEmpty(partialKey))
                        {
                            return "*****-*****-*****-*****-" + partialKey;
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки
            }

            return null;
        }

        static void EnumerateSPPKeys(string subKeyPath, List<string> foundKeys)
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var sppKey = baseKey.OpenSubKey(subKeyPath))
            {
                if (sppKey == null) return;
                PrintValuesRecursive(sppKey, foundKeys);
            }
        }

        static void PrintValuesRecursive(RegistryKey key, List<string> foundKeys)
        {
            foreach (var valName in key.GetValueNames())
            {
                var val = key.GetValue(valName);
                if (val is byte[] data && data.Length >= 0x34 + 15)
                {
                    var decoded = DecodeKey(data);
                    if (!string.IsNullOrEmpty(decoded))
                        foundKeys.Add($"Ключ из {key.Name}: {decoded}");
                }
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using (var subKey = key.OpenSubKey(subKeyName))
                {
                    if (subKey != null)
                        PrintValuesRecursive(subKey, foundKeys);
                }
            }
        }

        static void CheckAdditionalLocations(List<string> foundKeys)
        {
            var additionalPaths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Setup",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\Pid",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\Pid_InstallTime",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\Pid99"
            };

            foreach (var path in additionalPaths)
            {
                PrintRegistryValues(path, foundKeys);
            }
        }

        static void PrintRegistryValues(string subKeyPath, List<string> foundKeys)
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = baseKey.OpenSubKey(subKeyPath))
            {
                if (key == null) return;

                foreach (var valName in key.GetValueNames())
                {
                    var val = key.GetValue(valName);
                    if (val is byte[] data && data.Length >= 0x34 + 15)
                    {
                        var decoded = DecodeKey(data);
                        if (!string.IsNullOrEmpty(decoded))
                            foundKeys.Add($"Ключ из {subKeyPath}: {decoded}");
                    }
                }
            }
        }
    }
}
