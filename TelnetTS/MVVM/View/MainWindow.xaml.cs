using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
            Application.Current.Activated += CurrentOnActivated;
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
        private bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
               ? Application.Current.Windows.OfType<T>().Any()
               : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }
        private void CurrentOnActivated(object sender, EventArgs eventArgs)
        {
            if (IsWindowOpen<Window>("consoleWindow"))
            {
                var win = Application.Current.Windows.OfType<Window>().Where(w => w.Name.Equals("consoleWindow")).FirstOrDefault();
                win.WindowState = WindowState.Normal;
            }
            this.Activate();
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            var window = (Window)sender;
            if (window.WindowState == WindowState.Minimized && IsWindowOpen<Window>("consoleWindow"))
            {
                var win = Application.Current.Windows.OfType<Window>().Where(w => w.Name.Equals("consoleWindow")).FirstOrDefault();
                win.WindowState = WindowState.Minimized;
            }
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
            if (!IsWindowOpen<Window>("consoleWindow"))
            {
                var win = new ConsoleWindow(ViewModel);
                win.Show();
            }
            else
            {
                var win = Application.Current.Windows.OfType<Window>().Where(w => w.Name.Equals("consoleWindow")).FirstOrDefault();
                win.Show();
                win.WindowState = WindowState.Normal;
                win.Activate();
            }
        }

        private void ConnectPointIP_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed && ViewModel.OpenCommand.CanExecute(ViewModel.ConnectPoint))
                ViewModel.OpenCommand.Execute(null);
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingWindow();
            win.ShowDialog();
        }
    }
}
