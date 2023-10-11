using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TelnetTS.MVVM.ViewModel;

namespace TelnetTS.MVVM.View
{
    public partial class ConsoleWindow : Window
    {
        public ConsoleWindow(MainViewModel vm)
        {
            InitializeComponent();
            RadioPm.IsChecked = true;
            DataContext = vm;
        }
        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio != null && ResultTextbox != null)
            {
                var binding = new Binding($"{(string)radio.Tag}.TelnetResponse");
                ResultTextbox.SetBinding(TextBox.TextProperty, binding);
                MyScrollViewer.ScrollToBottom();
            }
        }

        private void consoleWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
