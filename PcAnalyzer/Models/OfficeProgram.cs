using Microsoft.Win32;
using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows;
using System.Reflection;

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
                string winProdKeyPath = ExtractEmbeddedResource("PcAnalyzer.ProductKeyScanner.exe");
                if (File.Exists(winProdKeyPath))
                {
                    Process.Start(winProdKeyPath);
                }
                else
                {
                    MessageBox.Show($"Файл {winProdKeyPath} не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
