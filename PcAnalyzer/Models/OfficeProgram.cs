using Binarysharp.MemoryManagement;
using Microsoft.Win32;
using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace PcAnalyzer.Models
{
    public class OfficeProgram : IExportable
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
        public string LicenceType { get; set; }
        public string Version { get; set; }
        public string SerialNumber { get; set; }

        public string FormatData()
        {
            return GetOfficeInfo();
        }
        public string GetName()
        {
            return "Office пакет";
        }
        static string GetOfficeInfo()
        {
            var officeDetails = new List<string>();

            var installedPackages = GetInstalledOfficeApplications();
            if (installedPackages.Count > 0)
            {
                foreach (var package in installedPackages)
                {
                    var licenseType = GetOfficeLicenseType(package);
                    var version = GetOfficeVersion(package);

                    officeDetails.Add($" - {package}");
                    officeDetails.Add($"   Тип лицензии: {licenseType}");
                    officeDetails.Add($"   Версия: {version}");

                    // Попытка извлечь ключ продукта
                    var officeKey = GetOfficeProductKey(package);
                    if (!string.IsNullOrEmpty(officeKey))
                    {
                        officeDetails.Add($"   Ключ продукта: {officeKey}");
                    }
                    else
                    {
                        officeDetails.Add("   Ключ продукта: Не удалось извлечь (возможно Click-to-Run или подписка)");
                    }

                    var installPath = GetOfficeInstallPath(package) ?? FindOfficeInstallationPath();
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        var officePrograms = GetOfficeApplicationsFromPath(installPath);
                        if (officePrograms.Count > 0)
                        {
                            foreach (var program in officePrograms)
                            {
                                officeDetails.Add($"     - {program.Name} (version: {program.Version})");
                            }
                        }
                    }
                }
            }

            var userOfficeDetails = GetUserOfficeApplications();
            if (userOfficeDetails.Count > 0)
            {
                officeDetails.Add("\nДополнительные версии Office, найденные в HKEY_CURRENT_USER:");
                officeDetails.AddRange(userOfficeDetails);
            }

            var memoryKeys = ScanMemoryForOfficeKeys();
            if (memoryKeys.Count > 0)
            {
                officeDetails.Add("\nКлючи, найденные в оперативной памяти:");
                officeDetails.AddRange(memoryKeys);
            }

            return officeDetails.Count > 0
                ? string.Join(Environment.NewLine, officeDetails)
                : "Microsoft Office продукты не найдены.";
        }

        static List<string> GetInstalledOfficeApplications()
        {
            var applications = new List<string>();

            try
            {
                var uninstallKeys = new[]
                {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

                foreach (var uninstallKey in uninstallKeys)
                {
                    using (var baseKey = Registry.LocalMachine.OpenSubKey(uninstallKey))
                    {
                        if (baseKey == null)
                            continue;

                        foreach (var subKeyName in baseKey.GetSubKeyNames())
                        {
                            using (var subKey = baseKey.OpenSubKey(subKeyName))
                            {
                                var displayName = subKey?.GetValue("DisplayName") as string;
                                if (!string.IsNullOrEmpty(displayName) && displayName.Contains("Microsoft Office"))
                                {
                                    applications.Add(displayName);
                                }
                                else if (!string.IsNullOrEmpty(displayName) && IsStandaloneOfficeApp(displayName))
                                {
                                    applications.Add(displayName);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения установленных приложений: {ex.Message}");
            }

            return applications;
        }

        static List<string> GetUserOfficeApplications()
        {
            var officeDetails = new List<string>();

            try
            {
                using (var baseKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Office"))
                {
                    if (baseKey != null)
                    {
                        foreach (var versionKey in baseKey.GetSubKeyNames())
                        {
                            var versionPath = baseKey.OpenSubKey(versionKey);
                            if (versionPath != null && decimal.TryParse(versionKey, out _))
                            {
                                officeDetails.Add($"Версия: {versionKey}");
                                foreach (var appKey in versionPath.GetSubKeyNames())
                                {
                                    officeDetails.Add($"  - {appKey} (найдено в версии: {versionKey})");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения HKEY_CURRENT_USER Office приложений: {ex.Message}");
            }

            return officeDetails;
        }

        static bool IsStandaloneOfficeApp(string displayName)
        {
            var standaloneApps = new[]
            {
            "Microsoft Word",
            "Microsoft Excel",
            "Microsoft PowerPoint",
            "Microsoft Outlook",
            "Microsoft Access",
            "Microsoft Publisher",
            "Microsoft OneNote",
            "Skype for Business",
            "Microsoft Teams"
        };

            foreach (var app in standaloneApps)
            {
                if (displayName.Contains(app))
                {
                    return true;
                }
            }

            return false;
        }

        static string GetOfficeLicenseType(string package)
        {
            string licenseType = "Неизвестно";

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration"))
                {
                    if (key != null)
                    {
                        var productType = key.GetValue("ProductReleaseIds") as string;
                        if (!string.IsNullOrEmpty(productType))
                        {
                            licenseType = productType.Contains("Subscription") ? "Подписка" : "Volume";
                        }
                    }
                }
            }
            catch
            {
                licenseType = "Неизвестно";
            }

            return licenseType;
        }

        static string GetOfficeVersion(string package)
        {
            string version = "Неизвестно";

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration"))
                {
                    if (key != null)
                    {
                        var versionValue = key.GetValue("VersionToReport") as string;
                        if (!string.IsNullOrEmpty(versionValue))
                        {
                            version = versionValue;
                            return version;
                        }
                    }
                }

                if (package.Contains("2010")) version = "14.0";
                else if (package.Contains("2013")) version = "15.0";
                else if (package.Contains("2016")) version = "16.0";
                else if (package.Contains("2019")) version = "16.0";
                else if (package.Contains("2021")) version = "16.0";
            }
            catch
            {
                version = "Неизвестно";
            }

            return version;
        }

        static string GetOfficeInstallPath(string package)
        {
            var installPaths = new[]
            {
            @"SOFTWARE\Microsoft\Office\16.0\Common\InstallRoot",
            @"SOFTWARE\Wow6432Node\Microsoft\Office\16.0\Common\InstallRoot"
        };

            foreach (var path in installPaths)
            {
                using (var key = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        var installPath = key.GetValue("Path") as string;
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            return installPath;
                        }
                    }
                }
            }

            return null;
        }

        static string FindOfficeInstallationPath()
        {
            var allDrives = DriveInfo.GetDrives();
            var possiblePaths = new[]
            {
            "Microsoft Office",
            "Microsoft Office\\root",
            "Common Files\\Microsoft Shared",
            "Program Files\\Microsoft Office"
        };

            foreach (var drive in allDrives)
            {
                if (drive.DriveType == DriveType.Fixed)
                {
                    foreach (var relativePath in possiblePaths)
                    {
                        var fullPath = Path.Combine(drive.RootDirectory.FullName, relativePath);
                        if (Directory.Exists(fullPath))
                        {
                            return fullPath;
                        }
                    }
                }
            }

            return null;
        }

        static List<(string Name, string Version)> GetOfficeApplicationsFromPath(string path)
        {
            var officePrograms = new List<(string Name, string Version)>();
            var programNames = new Dictionary<string, string>
        {
            { "WINWORD.EXE", "Microsoft Word" },
            { "EXCEL.EXE", "Microsoft Excel" },
            { "POWERPNT.EXE", "Microsoft PowerPoint" },
            { "OUTLOOK.EXE", "Microsoft Outlook" },
            { "MSACCESS.EXE", "Microsoft Access" },
            { "MSPUB.EXE", "Microsoft Publisher" },
            { "ONENOTE.EXE", "Microsoft OneNote" },
            { "Lync.exe", "Skype for Business" },
            { "Teams.exe", "Microsoft Teams" }
        };

            foreach (var program in programNames.Keys)
            {
                try
                {
                    var foundFiles = Directory.GetFiles(path, program, SearchOption.AllDirectories);
                    foreach (var file in foundFiles)
                    {
                        if (File.Exists(file))
                        {
                            var version = FileVersionInfo.GetVersionInfo(file).ProductVersion;
                            officePrograms.Add((programNames[program], version));
                        }
                    }
                }
                catch { }
            }

            return officePrograms;
        }

        static string GetOfficeProductKey(string package)
        {
            var version = GetOfficeVersion(package);

            if (version == "14.0" || version == "12.0" || version == "11.0")
            {
                return GetOldOfficeProductKeyFromRegistry(version);
            }

            return null;
        }

        static string GetOldOfficeProductKeyFromRegistry(string version)
        {
            var registrationPath = $@"SOFTWARE\Microsoft\Office\{version}\Registration";
            using (var baseKey = Registry.LocalMachine.OpenSubKey(registrationPath))
            {
                if (baseKey != null)
                {
                    foreach (var subKeyName in baseKey.GetSubKeyNames())
                    {
                        using (var regKey = baseKey.OpenSubKey(subKeyName))
                        {
                            if (regKey == null) continue;

                            var digitalProductId = regKey.GetValue("DigitalProductID") as byte[];
                            if (digitalProductId != null && digitalProductId.Length > 0)
                            {
                                return DecodeOfficeKey(digitalProductId);
                            }
                        }
                    }
                }
            }

            return null;
        }

        static string DecodeOfficeKey(byte[] digitalProductId)
        {
            const string keyChars = "BCDFGHJKMPQRTVWXY2346789";

            byte[] key = new byte[15];
            Array.Copy(digitalProductId, 0x34, key, 0, 15);

            char[] result = new char[29];
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
                        current = current % 24;
                    }
                    result[i] = keyChars[current];
                }
            }

            return new string(result);
        }

        public static List<string> ScanMemoryForOfficeKeys()
        {
            var keysFound = new List<string>();

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.MainModule == null || string.IsNullOrEmpty(process.MainModule.FileName))
                        continue;

                    if (!process.ProcessName.Contains("WINWORD") &&
                        !process.ProcessName.Contains("EXCEL") &&
                        !process.ProcessName.Contains("POWERPNT") &&
                        !process.ProcessName.Contains("OUTLOOK") &&
                        !process.ProcessName.Contains("MSACCESS"))
                    {
                        continue;
                    }

                    using (var memory = new MemorySharp(process))
                    {
                        foreach (var module in memory.Modules.RemoteModules)
                        {
                            try
                            {
                                var bytes = memory.Read<byte>(module.BaseAddress, module.Size);
                                var content = Encoding.ASCII.GetString(bytes);

                                string keyPattern = @"[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}";
                                var matches = Regex.Matches(content, keyPattern);

                                foreach (Match match in matches)
                                {
                                    keysFound.Add(match.Value);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            return keysFound;
        }
    }
}
