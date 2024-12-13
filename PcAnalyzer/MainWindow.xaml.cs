using PcAnalyzer.Interfaces;
using PcAnalyzer.Models;
using PcAnalyzer.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace PcAnalyzer
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }

        // Обработчики для выбора темы
        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeChanger.SwitchTheme("Light");
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeChanger.SwitchTheme("Dark");
        }

        // Обработчики для выбора шрифта
        private void FontArial_Click(object sender, RoutedEventArgs e)
        {
            ThemeChanger.SwitchFont("Arial");
        }

        private void FontSegoeUI_Click(object sender, RoutedEventArgs e)
        {
            ThemeChanger.SwitchFont("Segoe UI");
        }

        private void FontTahoma_Click(object sender, RoutedEventArgs e)
        {
            ThemeChanger.SwitchFont("Tahoma");
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (FormatComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string selectedFormat = selectedItem.Content.ToString();

                    if (selectedFormat == "TXT")
                    {
                        viewModel.ExportToTxt("PcInfo.txt");
                    }
                    else if (selectedFormat == "PDF")
                    {
                        viewModel.ExportToPdf("PcInfo.pdf");
                    }
                }
                else
                {
                    MessageBox.Show("Выберите формат для экспорта!");
                }
            }
            else
            {
                MessageBox.Show("Ошибка: DataContext не установлен!");
            }
        }

        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            var components = new List<IExportable>
            {
                ViewModel.Drive, ViewModel.GPU, ViewModel.PC, ViewModel.Network,
                ViewModel.CPU, ViewModel.RAM, ViewModel.OS, ViewModel.User,
                ViewModel.NetworkCard, ViewModel.OfficeProgram, ViewModel.OutlookProgram
            };

            if (SelectAllCheckBox.IsChecked == true) 
            {
                foreach (var item in components)
                {
                    item.IsChecked = true;
                }
            }
        }
        private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            var components = new List<IExportable>
            {
                ViewModel.Drive, ViewModel.GPU, ViewModel.PC, ViewModel.Network,
                ViewModel.CPU, ViewModel.RAM, ViewModel.OS, ViewModel.User,
                ViewModel.NetworkCard, ViewModel.OfficeProgram, ViewModel.OutlookProgram
            };

            if (SelectAllCheckBox.IsChecked == false)
            {
                foreach (var item in components)
                {
                    item.IsChecked = false;
                }
            }
        }
    }

    public static class ThemeChanger
    {
        public static void SwitchTheme(string theme)
        {
            // Получаем текущие словари ресурсов
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            // Удаляем предыдущую тему
            var existingTheme = dictionaries.FirstOrDefault(d => d.Source != null &&
                (d.Source.OriginalString.EndsWith("LightTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
                 d.Source.OriginalString.EndsWith("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase)));
            if (existingTheme != null)
            {
                dictionaries.Remove(existingTheme);
            }

            // Добавляем новую тему
            var newTheme = new ResourceDictionary();
            switch (theme)
            {
                case "Light":
                    newTheme.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
                    break;
                case "Dark":
                    newTheme.Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);
                    break;
                default:
                    newTheme.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
                    break;
            }

            dictionaries.Add(newTheme);
        }

        public static void SwitchFont(string fontName)
        {
            Application.Current.Resources["SelectedFontFamily"] = new System.Windows.Media.FontFamily(fontName);
        }
    }
}
