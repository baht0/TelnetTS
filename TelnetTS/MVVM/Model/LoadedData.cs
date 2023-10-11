using System;
using System.Windows;
using TelnetInfo.Core;

namespace TelnetTS.MVVM.Model
{
    public class Data
    {
        public string Nas { get; set; }
        public string Login { get; set; }
        public string Result { get; set; }
        public string Comment { get; set; }
        public string Date { get; set; }
        public string Mac { get; set; }
        public string Point { get; set; }

        public Data(string clipboard)
        {
            string[] stringSeparators = new string[] { "\r\n" };
            string[] lines = clipboard.Split(stringSeparators, StringSplitOptions.None);
            var columns = lines[0].Split('\t');
            var result = lines[1].Split('\t');

            for (int i = 0; i < columns.Length; i++)
            {
                switch (columns[i])
                {
                    case "NAS":
                        Nas = result[i];
                        break;
                    case "Логин":
                        Login = result[i];
                        break;
                    case "Результат":
                        Result = result[i];
                        break;
                    case "Комментарии":
                        Comment = result[i];
                        break;
                    case "Дата/время":
                        Date = result[i];
                        break;
                    case "MAC-адрес":
                        Mac = result[i];
                        break;
                    case "Точка подключения":
                        Point = result[i];
                        break;
                }
            }
        }
    }
    public class LoadedData : ObservableObject
    {
        public bool IsSuccess { get; set; } = true;
        private Data RadiusData { get; set; }

        public string Login
        {
            get => login;
            set
            {
                login = value;
                OnPropertyChanged();
            }
        }
        private string login = "-";
        public string DateTime
        {
            get => dateTime;
            set
            {
                dateTime = value;
                OnPropertyChanged();
            }
        }
        private string dateTime = "-";
        public MacAddressInfo MacAddressInfo
        {
            get => macAddressInfo;
            set
            {
                macAddressInfo = value;
                OnPropertyChanged();
            }
        }
        private MacAddressInfo macAddressInfo = new MacAddressInfo();
        public string Axiros
        {
            get => axiros;
            set
            {
                axiros = value;
                OnPropertyChanged();
            }
        }
        private string axiros = "-";

        public void LoadData(string clipboard)
        {
            try
            {
                RadiusData = new Data(clipboard);

                Login = RadiusData.Login;
                Axiros = "http://192.168.143.115/ttk/AXCustomerSupportPortal/#/cpes/by/CPESearchOptions.login/value/" + RadiusData.Login;
                DateTime = RadiusData.Date;
                MacAddressInfo.Main(RadiusData.Mac);
            }
            catch
            {
                MessageBox.Show("Проверьте правильность загружаемых данных!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                IsSuccess = false;
            }
        }

        public SessionData GetSessionData() => new SessionData(RadiusData.Nas, RadiusData.Login, RadiusData.Result);
        public ConnectPoint GetConnectPoint() => new ConnectPoint(RadiusData.Point, MacAddressInfo.Mac1);
    }
}
