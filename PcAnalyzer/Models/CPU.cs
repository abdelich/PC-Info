using PcAnalyzer.Interfaces;
using System.ComponentModel;

namespace PcAnalyzer.Models
{
    public class CPU : IExportable
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
        string Name { get; set; }
        string Model { get; set; }

        public string FormatData()
        {
            return GetCPUNameAndModel();
        }
        public string GetName()
        {
            return "Процессор";
        }
        string GetCPUNameAndModel()
        {
            string? CPUInfo = Convert.ToString(Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\HARDWARE\\DESCRIPTION\\SYSTEM\\CentralProcessor\\0", "ProcessorNameString", null));
            if (string.IsNullOrEmpty(CPUInfo))
            {
                return "Не удалось получить информацию о процессоре.";
            }
            else
            {
                // Проверяем, содержит ли строка ключевые слова, определяющие производителя
                string manufacturer = string.Empty;
                if (CPUInfo.Contains("Intel") || CPUInfo.Contains("Intel(R)") || CPUInfo.Contains("Intel (R)"))
                {
                    manufacturer = "Intel";
                }
                else if (CPUInfo.Contains("AMD"))
                {
                    manufacturer = "AMD";
                }

                // Отделяем модель процессора
                string model = CPUInfo.Replace(manufacturer, "").Trim();

                return "Производитель: " + manufacturer + "\nМодель: " + model;
            }
        }
    }
}
