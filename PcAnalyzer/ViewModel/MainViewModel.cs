using PcAnalyzer.Interfaces;
using PcAnalyzer.Models;
using System.ComponentModel;
using System.IO;
using System.Windows;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Text.RegularExpressions;
using System.Diagnostics;


namespace PcAnalyzer.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public Drive Drive { get; set; } = new Drive();
        public GPU GPU { get; set; } = new GPU();
        public PC PC { get; set; } = new PC();
        public Network Network { get; set; } = new Network();
        public CPU CPU { get; set; } = new CPU();
        public RAM RAM { get; set; } = new RAM();
        public OS OS { get; set; } = new OS();
        public User User { get; set; } = new User();
        public NetworkCard NetworkCard { get; set; } = new NetworkCard();
        public OfficeProgram OfficeProgram { get; set; } = new OfficeProgram();
        public OutlookProgram OutlookProgram { get; set; } = new OutlookProgram();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void ExportToTxt(string filePath)
        {
            var components = new List<IExportable>
            {
                Drive, GPU, PC, Network, CPU, RAM, OS, User, NetworkCard, OfficeProgram, OutlookProgram
            };
            if (components.Any(c => c.IsChecked))
            {
                var lines = new List<string>();
                foreach (var component in components)
                {
                    if (component.IsChecked)
                    {
                        lines.Add(component.GetName());
                        lines.Add($"\t{component.FormatData().Replace("\n", "\n\t")}");
                        lines.Add("-------------------------------\n");
                    }
                }

                File.WriteAllLines(filePath, lines);

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            else
                MessageBox.Show($"Выберите компоненты для экспорта");
        }
        public void ExportToPdf(string filePath)
        {
            try
            {
                var components = new List<IExportable>
        {
            Drive, GPU, PC, Network, CPU, RAM, OS, User, NetworkCard, OfficeProgram, OutlookProgram
        };
                if (components.Any(c => c.IsChecked))
                {
                    string textContent = GenerateTextContent();
                    textContent = CleanText(textContent);

                    PdfDocument document = new PdfDocument();
                    document.Info.Title = "Экспорт данных";

                    PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    XFont font = new XFont("Courier New", 12);

                    double yPoint = 20; // Начальная вертикальная позиция
                    double lineHeight = 12; // Высота строки
                    double margin = 20; // Поля страницы
                    double pageWidth = page.Width - margin * 2; // Ширина текста

                    var lines = textContent.Split('\n');

                    foreach (var line in lines)
                    {
                        string currentLine = line;

                        while (!string.IsNullOrEmpty(currentLine))
                        {
                            string drawLine = currentLine;

                            // Если строка слишком длинная, разделяем ее
                            while (gfx.MeasureString(drawLine, font).Width > pageWidth)
                            {
                                int splitIndex = drawLine.Length - 1;
                                while (gfx.MeasureString(drawLine.Substring(0, splitIndex), font).Width > pageWidth && splitIndex > 0)
                                {
                                    splitIndex--;
                                }

                                if (splitIndex > 0)
                                {
                                    drawLine = drawLine.Substring(0, splitIndex).TrimEnd();
                                }
                            }

                            if (yPoint + lineHeight > page.Height - margin)
                            {
                                page = document.AddPage();
                                gfx = XGraphics.FromPdfPage(page);
                                yPoint = margin;
                            }

                            gfx.DrawString(drawLine, font, XBrushes.Black, new XRect(margin, yPoint, pageWidth, page.Height), XStringFormats.TopLeft);
                            yPoint += lineHeight;

                            currentLine = currentLine.Length > drawLine.Length ? currentLine.Substring(drawLine.Length).TrimStart() : null;
                        }
                    }

                    document.Save(filePath);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                else
                    MessageBox.Show($"Выберите компоненты для экспорта");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в PDF: {ex.Message}");
            }
        }

        private string GenerateTextContent()
        {
            var components = new List<IExportable>
            {
                Drive, GPU, PC, Network, CPU, RAM, OS, User, NetworkCard, OfficeProgram, OutlookProgram
            };

            var lines = new List<string>();
            foreach (var component in components)
            {
                if (component.IsChecked)
                {
                    lines.Add(component.GetName());
                    lines.Add($"    {component.FormatData().Replace("\n", "\n    ")}");
                    lines.Add("-------------------------------\n");
                }
            }

            return string.Join("\n", lines);
        }


        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            input = input.Replace("\t", "    ");

            input = input.Replace("\u00A0", " ");

            input = Regex.Replace(input, @"[^\u0020-\u007E\u0400-\u04FF\n ]", "");

            return input.Trim();
        }
    }
}
