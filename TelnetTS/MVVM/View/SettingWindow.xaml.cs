using System.Windows;
using TelnetTS.MVVM.ViewModel;

namespace TelnetTS.MVVM.View
{
    /// <summary>
    /// Interaction logic for SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        private readonly SettingViewModel ViewModel = new SettingViewModel();
        public SettingWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
