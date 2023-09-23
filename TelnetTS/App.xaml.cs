using System.Windows;
using TelnetTS.MVVM.View;

namespace TelnetTS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void ApplicationStart(object sender, StartupEventArgs e)
        {
            Window start = new MainWindow();
            start.Show();
        }
    }
}
