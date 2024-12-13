using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Management;

namespace PcAnalyzer.Models
{
    public class PC : IExportable
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
        public string Model { get; set; }
        public string Firm { get; set; }

        public string FormatData()
        {
            return GetPCInfo();
        }
        public string GetName()
        {
            return "ПК";
        }
        static string GetPCInfo()
        {
            string manufacturer = string.Empty;
            string model = string.Empty;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    manufacturer = obj["Manufacturer"]?.ToString() ?? "Неизвестно";
                    model = obj["Model"]?.ToString() ?? "Неизвестно";
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка при получении данных: {ex.Message}";
            }

            return $"Фирма: {manufacturer}\nМодель: {model}";
        }
    }
}
