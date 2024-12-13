using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Management;
using System.Net;
using System.Text;

namespace PcAnalyzer.Models
{
    public class Network : IExportable
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); public string Domen {  get; set; }
        public string Group { get; set; }
        public string NetworkName { get; set; }

        public string FormatData()
        {
            return GetDomainGroupName();
        }
        public string GetName()
        {
            return "Сеть";
        }
        string GetDomainGroupName()
        {
            StringBuilder result = new StringBuilder();
            string computerName = Dns.GetHostName();
            result.AppendLine($"Имя компьютера: {computerName}");

            string domainName = "-";
            string workgroupName = "-";

            try
            {
                // Используем WMI для получения информации о рабочей группе или домене
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT DomainRole, Domain FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        // Проверяем роль компьютера в сети (0 и 2 = рабочая группа, остальные = домен)
                        int domainRole = Convert.ToInt32(queryObj["DomainRole"]);
                        string domainOrWorkgroup = queryObj["Domain"]?.ToString();

                        if (domainRole == 0 || domainRole == 2) // Рабочая группа
                        {
                            workgroupName = domainOrWorkgroup ?? "-";
                        }
                        else // Домен
                        {
                            domainName = domainOrWorkgroup ?? "-";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"Ошибка получения данных через WMI: {ex.Message}");
            }

            result.AppendLine($"Домен: {domainName}");
            result.AppendLine($"Рабочая группа: {workgroupName}");

            return result.ToString();
        }
    }
}
