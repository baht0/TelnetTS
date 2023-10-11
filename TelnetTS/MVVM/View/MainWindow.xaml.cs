using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using TelnetTS.MVVM.ViewModel;

namespace TelnetTS.MVVM.View
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel ViewModel = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.DisconectCommand.Execute(null);
            Application.Current.Shutdown();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Space || e.Key == Key.Enter) && MainButton.IsEnabled)
                ViewModel.RequestCommand.Execute(ViewModel.Allowed);
        }

        private void dataCopy_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Clipboard.SetText(((TextBlock)sender)?.Text);

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri.OriginalString != "-")
            {
                var psi = new ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(psi);
                e.Handled = true;
            }
        }
        private void ConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new ConsoleWindow(ViewModel);
            win.ShowDialog();
        }

        private void ConnectPointIP_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed && ViewModel.ConnectPoint.OpenCommand.CanExecute(ViewModel.ConnectPoint))
                ViewModel.ConnectPoint.OpenCommand.Execute(null);
        }
        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingWindow();
            win.ShowDialog();
        }
    }
}
