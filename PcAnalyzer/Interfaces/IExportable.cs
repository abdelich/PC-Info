using System.ComponentModel;

namespace PcAnalyzer.Interfaces
{
    public interface IExportable : INotifyPropertyChanged
    {
        public string FormatData();
        public bool IsChecked {  get; set; }
        public string GetName();
    }
}
