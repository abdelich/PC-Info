using System.Windows;

namespace PcAnalyzer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var splashScreen = new SplashScreen("Images/lab_01_blue.png");
            splashScreen.Show(true);
            Thread.Sleep(2000);

            base.OnStartup(e);
        }
    }
}
