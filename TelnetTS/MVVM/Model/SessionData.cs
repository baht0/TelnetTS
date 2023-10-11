using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TelnetInfo.Core;
using TelnetTS.Helper;

namespace TelnetTS.MVVM.Model
{
    public partial class SessionData : BaseModel
    {
        private string User => ProviderEdge.User;
        private string Pass => ProviderEdge.Pass;
        private Dictionary<int, string> ListPE { get; set; }

        private string Login { get; set; }
        public string Nas
        {
            get => nas;
            set
            {
                nas = value;
                OnPropertyChanged();
            }
        }
        private string nas = "-";
        public int NasId
        {
            get => nasId;
            set
            {
                nasId = value;
                OnPropertyChanged();
            }
        }
        private int nasId = 0;
        public string IPv4
        {
            get => iPv4;
            set
            {
                iPv4 = value.Trim();
                OnPropertyChanged();
            }
        }
        private string iPv4 = "-";
        public double Rate
        {
            get => rate;
            set
            {
                rate = value;
                OnPropertyChanged();
            }
        }
        private double rate;

        public SessionData()
        {
            //
        }
        public SessionData(string nas, string login, string result)
        {
            Login = login;
            GetIpEquipment(nas);
            IsSuccess = result == "Успешно" && !string.IsNullOrEmpty(IP);
        }
        private void GetIpEquipment(string str)
        {
            var number = str.Substring(str.LastIndexOf('-') + 1, 2);
            int.TryParse(number, out int id);
            if (str.Contains("KZN"))
            {
                ListPE = ProviderEdge.KZN;
                Nas = nameof(ProviderEdge.KZN);
                NasId = id;
                IP = ListPE.FirstOrDefault(x => x.Key == id).Value;
            }
            else if (str.Contains("CHE"))
            {
                ListPE = ProviderEdge.CHE;
                Nas = nameof(ProviderEdge.CHE);
                NasId = id;
                IP = ListPE.FirstOrDefault(x => x.Key == id).Value;
            }
            else if (str.Contains("ALM"))
            {
                ListPE = ProviderEdge.ALM;
                Nas = nameof(ProviderEdge.ALM);
                NasId = id;
                IP = ListPE.FirstOrDefault(x => x.Key == id).Value;
            }
            else
                IP = null;
        }

        //Launch
        public async void Main()
        {
            try
            {
                IsBusy = true;
                if (IsSuccess)
                {
                    await Connection();
                    await CheckRequest();
                }
                else
                    LogsAdd("Сессия не проверена.");
            }
            finally
            {
                IsBusy = false;
            }
        }
        //Connection
        private async Task Connection()
        {
            try
            {
                await TClient.ConnectionAsync(IP, User, Pass);
                SetTimer(min: 5);
            }
            catch
            {
                LogsAdd($"Ошибка при подключению к {Nas}_{NasId}");
            }
        }
        //Check`
        private async Task CheckRequest()
        {
            try
            {
                if (TClient.IsConneted)
                {
                    await TClient.SendAsync($"show subscriber session username {Login}", "c");
                    await CheckResponse();
                }
            }
            catch
            {
                LogsAdd("Не удалось проверить результат сессии.");
            }
        }
        private async Task CheckResponse()
        {
            bool isReseted = false;
            while (true)
            {
                var line = await TClient.ReadAsync();
                if (line == null) break;

                if(line.Contains("show subscriber session username"))
                    Copy.AppendLine(line);

                TelnetResponse += line;
                if (line.Contains("authen") || IsActive)
                {
                    IsActive = true;
                    if (line.Contains("IPv4"))
                        IPv4 = line.Replace("IPv4 Address: ", string.Empty);
                    if (line.Contains("Up-time"))
                    {
                        string[] times = line.Split(',');
                        var time = times[0].Replace("Session Up-time: ", string.Empty);
                        UpTime = TimeSpanParser.Main(time);
                    }
                    if (line.Contains("ISG_Internet_") && !line.Contains("NoAccounting"))
                    {
                        var value = Regex.Match(line, @"\d+").Value;
                        double.TryParse(value, out double result);
                        Rate = result;
                    }
                    else if (line.Contains("L4R_NOBALANCE_SERVICE2") || line.Contains("ISG_OGARDEN_SERVICE2"))
                        isReseted = true;
                    Copy.AppendLine(line);
                }
            }
            if (isReseted)
            {
                LogsAdd("На оборудование блокировка по оплате.");
                ResetRequestCommand.Execute(null);
            }
        }

        public ICommand CopyCommand =>
            new RelayCommand((obj) =>
            {
                Clipboard.SetText(Copy.ToString());
            }, (obj) => !IsBusy && !string.IsNullOrEmpty(Copy.ToString()));
        public ICommand ResetRequestCommand =>
            new RelayCommand(async (obj) =>
            {
                await TClient.SendAsync($"clear subscriber session username {Login}");
                LogsAdd("Сессия была сброшена.", false);
                IsActive = false;
                Rate = 0;
                IPv4 = "-";
                UpTime = new TimeSpan();
                AddTimeToTimer(min: 5);

            }, (obj) => IsActive && !IsBusy && TClient.IsConneted);
        public ICommand RefreshRequestCommand =>
            new RelayCommand(async (obj) =>
            {
                IsBusy = true;
                Copy.Clear();
                LogsClear();

                IsActive = false;
                Rate = 0;
                IPv4 = "-";
                UpTime = new TimeSpan();

                if (IsSuccess && TClient.IsConneted)
                {
                    await CheckRequest();
                    if (IsActive)
                    {
                        AddTimeToTimer(min: 5);
                        IsBusy = false;
                        return;
                    }
                }

                var res = MessageBox.Show(
                    $"Сессия в {Nas}_{NasId} не проверена.\n\nНачать поиск на всех PE в этой зоне?", "Поиск сессии",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    foreach (var e in ListPE)
                    {
                        IsSuccess = true;
                        NasId = e.Key;
                        IP = e.Value;
                        await Connection();
                        await CheckRequest();
                        if (IsActive)
                            break;
                        else
                            Disconnect();
                    }
                    if (!IsActive)
                        LogsAdd($"Сессия в зоне '{Nas}' не найдена.", false);
                }
                IsBusy = false;

            }, (obj) => !IsBusy && !string.IsNullOrEmpty(Nas) && Nas != "-");
    }
}
