using Microsoft.Win32;
using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

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
            static string ExtractEmbeddedResource(string resourceName)
            {
                string tempPath = Path.Combine(Path.GetTempPath(), resourceName);
                using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                        throw new FileNotFoundException($"Ресурс {resourceName} не найден.");

                    using (FileStream fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }
                return tempPath;
            }
            try
            {
                string winKeyFinderPath = ExtractEmbeddedResource("PcAnalyzer.WinKeyFinder.exe");
                if (File.Exists(winKeyFinderPath))
                {
                    Process.Start(winKeyFinderPath);
                }
                else
                {
                    MessageBox.Show($"Файл {winKeyFinderPath} не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return "В приложении WinKeyFinder";
        }
    }
}
