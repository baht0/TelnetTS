using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TelnetTS.MVVM.ViewModel;

namespace TelnetTS.MVVM.View
{
    public partial class SettingWindow : Window
    {
        private readonly SettingViewModel ViewModel = new SettingViewModel();
        public SettingWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
