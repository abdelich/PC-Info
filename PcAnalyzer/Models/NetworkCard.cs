using PcAnalyzer.Interfaces;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.ComponentModel;
using System.Text;

namespace PcAnalyzer.Models
{
    public class NetworkCard : IExportable
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
        public string Ip { get; set; }
        public string MAC { get; set; }

        public string FormatData()
        {
            return GetNetworkCardInfo();
        }
        public string GetName()
        {
            return "Сетевые карты";
        }
        static string GetNetworkCardInfo()
        {
            StringBuilder output = new StringBuilder();
            int cardNumber = 1;

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    string description = nic.Description;
                    string manufacturer = ExtractManufacturer(description);
                    string model = ExtractModel(description);
                    string macAddress = GetMACAddress(nic);
                    string localIPAddress = GetLocalIPAddress(nic);
                    string publicIPAddress = GetPublicIPAddress();

                    output.AppendLine($"    Карта {cardNumber}:");
                    output.AppendLine($"        Фирма: {manufacturer}");
                    output.AppendLine($"        Модель: {model}");
                    output.AppendLine($"        MAC-адрес: {macAddress}");
                    output.AppendLine($"        Локальный IP: {localIPAddress ?? "Не найден"}");
                    output.AppendLine($"        Публичный IP: {publicIPAddress}");
                    cardNumber++;
                }
            }

            return output.ToString();
        }

        static string ExtractManufacturer(string description)
        {
            // Попытка выделить производителя из модели
            var match = System.Text.RegularExpressions.Regex.Match(description, @"\b(Intel|Realtek|Broadcom|Qualcomm|TP-Link|Asus|Atheros|Marvell|Dell|Microsoft)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Value : "Неизвестный производитель";
        }

        static string ExtractModel(string description)
        {
            // Убираем производителя из описания модели
            string manufacturer = ExtractManufacturer(description);
            return description.Replace(manufacturer, "").Trim();
        }

        static string GetMACAddress(NetworkInterface nic)
        {
            return string.Join(":", nic.GetPhysicalAddress()
                                       .GetAddressBytes()
                                       .Select(b => b.ToString("X2")));
        }

        static string GetLocalIPAddress(NetworkInterface nic)
        {
            var ipProperties = nic.GetIPProperties();
            var ipAddress = ipProperties.UnicastAddresses
                .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(addr => addr.Address.ToString())
                .FirstOrDefault();
            return ipAddress;
        }

        static string GetPublicIPAddress()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    return httpClient.GetStringAsync("https://api.ipify.org").Result;
                }
            }
            catch
            {
                return "Не удалось получить публичный IP-адрес.";
            }
        }
    }
}
