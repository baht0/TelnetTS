using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using TelnetInfo.Core;
using TelnetTS.Helper;

namespace TelnetTS.MVVM.Model
{
    public partial class SessionData : BaseModel
    {
        private string User => ProviderEdge.User;
        private string Pass => ProviderEdge.Pass;

        private Dictionary<int, string> KZN => ProviderEdge.KZN;
        private Dictionary<int, string> CHE => ProviderEdge.CHE;
        private Dictionary<int, string> ALM => ProviderEdge.ALM;

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
            if (TClient != null && TClient.IsConneted)
                TClient.Disconnect();
            IsSuccess = false;
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
                Nas = nameof(KZN);
                NasId = id;
                IP = KZN.FirstOrDefault(x => x.Key == id).Value;
            }
            else if (str.Contains("CHE"))
            {
                Nas = nameof(CHE);
                NasId = id;
                IP = CHE.FirstOrDefault(x => x.Key == id).Value;
            }
            else if (str.Contains("ALM"))
            {
                Nas = nameof(ALM);
                NasId = id;
                IP = ALM.FirstOrDefault(x => x.Key == id).Value;
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
                IsSuccess = false;
                LogsAdd("Ошибка при подключению к оборудованию.");
            }
        }
        //Check
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
                }
            }
            if (isReseted)
            {
                LogsAdd("На оборудование блокировка по оплате.");
                ResetRequestCommand.Execute(null);
            }
        }

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
                IsActive = false;
                Rate = 0;
                IPv4 = "-";
                UpTime = new TimeSpan();
                LogsClear();
                await CheckRequest();
                AddTimeToTimer(min: 5);

            }, (obj) => !IsBusy && IsSuccess && !string.IsNullOrEmpty(IP) && IP != "-" && TClient.IsConneted);
        public ICommand FindRequestCommand =>
            new RelayCommand(async (obj) =>
            {
                var list = new Dictionary<int, string>();
                switch (Nas)
                {
                    case "KZN":
                        list = KZN; break;
                    case "CHE":
                        list = CHE; break;
                    case "ALM":
                        list = ALM; break;
                }

                foreach (var e in list)
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
                    LogsAdd("Сессия не найдена.", false);
            }, (obj) => !IsActive && !IsBusy && !string.IsNullOrEmpty(Nas) && Nas != "-");
    }
}
