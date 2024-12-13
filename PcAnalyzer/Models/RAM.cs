using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Management;
using System.Text;

namespace PcAnalyzer.Models
{
    public class RAM : IExportable
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
        public Dictionary<int, List<string>> PlankFirmSize { get; set; }
        public int Size { get; set; }

        public string FormatData()
        {
            return GetMemoryInfo();
        }
        public string GetName()
        {
            return "Оперативная память";
        }
        static string GetMemoryInfo()
        {
            StringBuilder output = new StringBuilder();
            ulong totalMemory = 0;
            int memoryStickNumber = 1;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string manufacturer = obj["Manufacturer"]?.ToString() ?? "Неизвестно";
                    ulong capacity = (ulong)(obj["Capacity"] ?? 0);
                    totalMemory += capacity;

                    output.AppendLine($"    Плашка {memoryStickNumber}:");
                    output.AppendLine($"        Фирма: {manufacturer}");
                    output.AppendLine($"        Объем памяти: {capacity / (1024 * 1024)} MB");
                    memoryStickNumber++;
                }

                output.AppendLine($"\n    Общий объем оперативной памяти: {totalMemory / (1024 * 1024)} MB");
            }
            catch (Exception ex)
            {
                output.Clear();
                output.AppendLine($"Ошибка при получении данных: {ex.Message}");
            }

            return output.ToString();
        }
    }
}
