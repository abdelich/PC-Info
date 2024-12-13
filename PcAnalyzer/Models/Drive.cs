using PcAnalyzer.Interfaces;
using System.Text;
using System.Management;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace PcAnalyzer.Models
{
    public class Drive : IExportable
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
        public string Type { get; set; }
        public int Size { get; set; }
        public string Firm { get; set; }
        public string Model { get; set; }

        static Dictionary<string, string> manufacturers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Samsung", "Samsung" },
                { "Seagate", "Seagate" },
                { "Western Digital", "Western Digital" },
                { "Intel", "Intel" },
                { "Kingston", "Kingston" },
                { "Crucial", "Crucial" },
                { "Toshiba", "Toshiba" },
                { "Hitachi", "Hitachi" },
                { "ST", "Seagate" },
                { "WDC", "Western Digital" },
                { "WD", "Western Digital" },
                { "HGST", "Hitachi" },
                { "SanDisk", "SanDisk" },
                { "Micron", "Micron" },
                { "ADATA", "ADATA" },
                { "PNY", "PNY" }
            };

        public string FormatData()
        {
            return GetDiskInfo();
        }
        public string GetName()
        {
            return "Диски";
        }
        static string GetDiskInfo()
        {
            StringBuilder diskInfo = new StringBuilder();
            int diskNumber = 1;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string model = queryObj["Model"]?.ToString() ?? "Не указано";
                    string mediaType = queryObj["MediaType"]?.ToString() ?? "Не указано";
                    string manufacturer = queryObj["Manufacturer"]?.ToString() ?? "Не указано";

                    if (manufacturer == "Не указано" || manufacturer == "(Стандартные дисковые накопители)")
                    {
                        manufacturer = ExtractManufacturerFromModel(model);
                    }

                    string typeFromModel = ExtractTypeFromModel(model);

                    if (string.IsNullOrEmpty(mediaType) || mediaType == "Не указано" || mediaType.Contains("Fixed", StringComparison.OrdinalIgnoreCase))
                    {
                        mediaType = typeFromModel;
                    }
                    else
                    {
                        mediaType = NormalizeMediaType(mediaType);
                    }

                    string cleanedModel = RemoveTypeAndManufacturerFromModel(model, manufacturer);

                    diskInfo.AppendLine($"Диск {diskNumber}:");
                    diskInfo.AppendLine($"    Тип: {mediaType}");
                    diskInfo.AppendLine($"    Размер: {Math.Round(Convert.ToDouble(queryObj["Size"]) / 1024 / 1024 / 1024, 0)} ГБ");
                    diskInfo.AppendLine($"    Фирма: {manufacturer}");
                    diskInfo.AppendLine($"    Модель: {cleanedModel}");
                    diskNumber++;
                }
            }
            catch (Exception e)
            {
                diskInfo.AppendLine("Ошибка: " + e.Message);
            }

            return diskInfo.ToString();
        }

        static string ExtractManufacturerFromModel(string model)
        {

            foreach (var kvp in manufacturers)
            {
                if (model.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return "Неизвестный производитель";
        }

        static string ExtractTypeFromModel(string model)
        {
            if (model.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                return "NVMe";
            if (model.Contains("SSD", StringComparison.OrdinalIgnoreCase))
                return "SSD";
            if (model.Contains("HDD", StringComparison.OrdinalIgnoreCase))
                return "HDD";

            return "HDD";
        }

        static string NormalizeMediaType(string mediaType)
        {
            if (mediaType.Contains("Fixed", StringComparison.OrdinalIgnoreCase))
                return "HDD";
            if (mediaType.Contains("Removable", StringComparison.OrdinalIgnoreCase))
                return "USB";
            if (mediaType.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                return "NVMe";
            if (mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase))
                return "SSD";
            return mediaType;
        }

        static string RemoveTypeAndManufacturerFromModel(string model, string manufacturer)
        {
            string cleanedModel = Regex.Replace(model, @"\b(NVMe|SSD|HDD)\b", "", RegexOptions.IgnoreCase).Trim();

            foreach (var kvp in manufacturers)
            {
                cleanedModel = Regex.Replace(cleanedModel, $@"\b{Regex.Escape(kvp.Key)}\b", "", RegexOptions.IgnoreCase).Trim();
            }

            if (!string.IsNullOrEmpty(manufacturer) && manufacturer != "Неизвестный производитель")
            {
                cleanedModel = Regex.Replace(cleanedModel, $@"\b{Regex.Escape(manufacturer)}\b", "", RegexOptions.IgnoreCase).Trim();
            }

            cleanedModel = Regex.Replace(cleanedModel, @"\s{2,}", " ").Trim();

            return cleanedModel;
        }
    }
}
