using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TelnetInfo.Core;
using TelnetTS.MVVM.Model;
using TelnetTS.Properties;

namespace TelnetTS.MVVM.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        public bool Allowed { get; set; } = true;
        public LoadedData Data
        {
            get => data;
            set
            {
                data = value;
                OnPropertyChanged();
            }
        }
        private LoadedData data = new LoadedData();
        public SessionData Session
        {
            get => session;
            set
            {
                session = value;
                OnPropertyChanged();
            }
        }
        private SessionData session = new SessionData();
        public ConnectPoint ConnectPoint
        {
            get => connectPoint;
            set
            {
                connectPoint = value;
                OnPropertyChanged();
            }
        }
        private ConnectPoint connectPoint = new ConnectPoint();

        private async void DisableBtn()
        {
            Allowed = false;
            for (int i = 0; i < 5; i++)
                await Task.Delay(1000);
            Allowed = true;
        }
        public ICommand RequestCommand =>
            new RelayCommand((obj) =>
            {
                Disconnect();

                Data = new LoadedData();
                Session = new SessionData();
                ConnectPoint = new ConnectPoint();

                var clipboard = Clipboard.GetText();
                Data.LoadData(clipboard);

                if (Data.IsSuccess)
                {
                    DisableBtn();
                    Session = Data.GetSessionData();
                    ConnectPoint = Data.GetConnectPoint();

                    Session.Main();
                    ConnectPoint.Main();
                }
            }, (obj) => Allowed);

        public ICommand OpenCommand =>
            new RelayCommand((obj) =>
            {
                var pathExe = Settings.Default.PathExe;
                var startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = $"/C {pathExe} /nomenu /telnet {ConnectPoint.IP}"
                };
                var process = new Process
                {
                    StartInfo = startInfo
                };
                process.Start();

            }, (obj) => !string.IsNullOrEmpty(Settings.Default.PathExe) && ConnectPoint != null && ConnectPoint.IP != "-");
        public ICommand PingCommand =>
            new RelayCommand((obj) =>
            {
                Process.Start("cmd.exe", $"/K ping {ConnectPoint.IP} -t");
            }, (obj) => ConnectPoint != null && ConnectPoint.IP != "-");
        public ICommand DisconectCommand => new RelayCommand((obj) => Disconnect());
        private void Disconnect()
        {
            Session.Disconnect();
            ConnectPoint.Disconnect();
        }
    }
}
