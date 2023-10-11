using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TelnetTS.MVVM.Model;

namespace TelnetTS.MVVM.View
{
    public partial class IncidentWindow : Window
    {
        private IncidentData Data { get; set; }
        public IncidentWindow(IncidentData incident)
        {
            InitializeComponent();
            Data = incident;
            this.DataContext = incident;
        }
        private void dataCopy_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Clipboard.SetText(((TextBlock)sender)?.Text);

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var str = new StringBuilder();
            str.AppendLine($"ID: {Data.ID}");
            str.AppendLine($"ЗУЭС: {Data.Zues}");
            str.AppendLine($"РУЭС: {Data.Rues}");
            str.AppendLine($"Адрес: {Data.Address}");
            str.AppendLine($"Дата начала: {Data.DataStart}");
            str.AppendLine($"Проблема: {Data.Problem}");
            str.AppendLine($"Всего услуг в простое: {Data.UslugiAll}");
            str.AppendLine($"IP: {Data.IP}");
            Clipboard.SetText(str.ToString());
        }
    }
}
