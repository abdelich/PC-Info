using PcAnalyzer.Interfaces;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Text;

namespace PcAnalyzer.Models
{
    public class User : IExportable
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
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }

        public string FormatData()
        {
            return GetUserAccounts();
        }
        public string GetName()
        {
            return "Пользователи";
        }
        static string GetUserAccounts()
        {
            StringBuilder output = new StringBuilder();
            int userNumber = 1;

            try
            {
                // Получаем информацию о пользователях в локальной системе
                using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                {
                    using (PrincipalSearcher searcher = new PrincipalSearcher(new UserPrincipal(context)))
                    {
                        foreach (var result in searcher.FindAll())
                        {
                            UserPrincipal user = result as UserPrincipal;
                            if (user != null)
                            {
                                // Исключаем стандартные учетные записи Windows
                                if (IsDefaultWindowsAccount(user.SamAccountName))
                                    continue;

                                output.AppendLine($"Пользователь {userNumber}:");
                                output.AppendLine($"    Имя: {user.Name}");
                                output.AppendLine($"    Логин: {user.SamAccountName}");
                                output.AppendLine($"    Домен: {GetDomainForUser(user)}");
                                output.AppendLine($"    Пароль: Невозможно получить (хранится в зашифрованном виде)");
                                userNumber++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                output.Clear();
                output.AppendLine($"Ошибка при получении данных: {ex.Message}");
            }

            return output.ToString();
        }

        static bool IsDefaultWindowsAccount(string accountName)
        {
            // Список стандартных учетных записей Windows
            string[] defaultAccounts = 
                {
                "Administrator", "Guest", "DefaultAccount", "WDAGUtilityAccount"
                };

            // Проверяем, является ли имя учетной записи стандартным
            foreach (string defaultAccount in defaultAccounts)
            {
                if (string.Equals(accountName, defaultAccount, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static string GetDomainForUser(UserPrincipal user)
        {
            try
            {
                // Если учетная запись локальная, возвращаем имя машины (локальный контекст)
                return user.Context.Name ?? "Локальная учетная запись";
            }
            catch
            {
                return "Неизвестный домен";
            }
        }
    }
}
