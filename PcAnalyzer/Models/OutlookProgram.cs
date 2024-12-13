using Microsoft.Win32;
using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System;
using Microsoft.Office.Interop.Outlook;

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
            string progID = GetAvailableOutlookProgID();
            if (progID == null)
            {
                return "Outlook не установлен или не поддерживается.";
            }

            StringBuilder result = new StringBuilder();
            result.AppendLine($"Outlook версия: {progID}");

            // Получение аккаунтов
            result.AppendLine("Аккаунты в Outlook:");
            try
            {
                Type outlookType = Type.GetTypeFromProgID(progID);
                dynamic outlookApp = Activator.CreateInstance(outlookType);
                dynamic outlookNamespace = outlookApp.GetNamespace("MAPI");

                for (int i = 1; i <= outlookNamespace.Folders.Count; i++)
                {
                    dynamic folder = outlookNamespace.Folders[i];
                    result.AppendLine($"- {folder.Name}");
                }
            }
            catch (System.Exception ex)
            {
                result.AppendLine($"Ошибка при получении аккаунтов: {ex.Message}");
            }

            // Получение данных файлов
            result.AppendLine("Файлы данных Outlook:");
            try
            {
                Type outlookType = Type.GetTypeFromProgID(progID);
                dynamic outlookApp = Activator.CreateInstance(outlookType);
                dynamic outlookNamespace = outlookApp.GetNamespace("MAPI");

                foreach (dynamic store in outlookNamespace.Stores)
                {
                    result.AppendLine($"Хранилище: {store.DisplayName}");
                    result.AppendLine($"Файл данных: {store.FilePath}");
                }
            }
            catch (System.Exception ex)
            {
                result.AppendLine($"Ошибка при получении файлов данных: {ex.Message}");
            }

            return result.ToString();
        }
        public string GetName()
        {
            return "Outlook";
        }

        static string GetAvailableOutlookProgID()
        {
            string[] outlookProgIDs = {
            "Outlook.Application.16", // Outlook 2016/2019/365
            "Outlook.Application.15", // Outlook 2013
            "Outlook.Application.14", // Outlook 2010
            "Outlook.Application"     // Любая версия (fallback)
        };

            foreach (var progID in outlookProgIDs)
            {
                try
                {
                    Type outlookType = Type.GetTypeFromProgID(progID);
                    if (outlookType != null)
                    {
                        return progID; // Возвращаем найденный ProgID
                    }
                }
                catch
                {
                    // Игнорируем ошибки и переходим к следующему ProgID
                }
            }

            return null; // Если ни один ProgID не найден
        }

        public static void GetOutlookAccounts(string progID)
        {
            try
            {
                // Создаем объект Outlook через позднее связывание
                Type outlookType = Type.GetTypeFromProgID(progID);
                if (outlookType == null)
                {
                    Console.WriteLine("Outlook не установлен.");
                    return;
                }

                dynamic outlookApp = Activator.CreateInstance(outlookType);
                dynamic outlookNamespace = outlookApp.GetNamespace("MAPI");

                Console.WriteLine("Аккаунты в Outlook:");
                for (int i = 1; i <= outlookNamespace.Folders.Count; i++)
                {
                    dynamic folder = outlookNamespace.Folders[i];
                    Console.WriteLine($"- Аккаунт: {folder.Name}");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Ошибка при работе с Outlook: {ex.Message}");
            }
        }

        public static void GetOutlookDataFiles(string progID)
        {
            try
            {
                // Создаем объект Outlook через позднее связывание
                Type outlookType = Type.GetTypeFromProgID(progID);
                if (outlookType == null)
                {
                    Console.WriteLine("Outlook не установлен.");
                    return;
                }

                dynamic outlookApp = Activator.CreateInstance(outlookType);
                dynamic outlookNamespace = outlookApp.GetNamespace("MAPI");

                foreach (dynamic store in outlookNamespace.Stores)
                {
                    Console.WriteLine($"Хранилище: {store.DisplayName}");
                    Console.WriteLine($"Файл данных: {store.FilePath}");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Ошибка при получении файлов данных: {ex.Message}");
            }
        }
    }
}
