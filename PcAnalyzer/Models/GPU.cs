using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Management;
using System.Text;

namespace PcAnalyzer.Models
{
    public class GPU : IExportable
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
        public string Firm {  get; set; }
        public string Model { get; set; }

        public string FormatData()
        {
            return GetGPUFirmAndModel();
        }
        public string GetName()
        {
            return "Видеокарты";
        }

        static string GetGPUFirmAndModel()
        {
            StringBuilder result = new StringBuilder();
            int gpuNumber = 1;

            using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "Unknown";
                    string firm = "";
                    string model = "";

                    string[] parts = name.Split(' ');
                    if (parts.Length > 1)
                    {
                        firm = parts[0].Replace("(R)", "");
                        model = string.Join(" ", parts.Skip(1)); // Остальное как модель
                    }
                    else
                    {
                        firm = name; // Если нет разделения, вся строка считается фирмой
                    }

                    result.AppendLine($"Видеокарта {gpuNumber}");
                    result.AppendLine($"    Фирма: {firm}");
                    result.AppendLine($"    Модель: {model}");
                    gpuNumber++;
                }
            }
            return result.ToString();
        }
    }
}
