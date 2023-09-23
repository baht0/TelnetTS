using System;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TelnetInfo.Core;
using TelnetTS.Helper;
using TelnetTS.Properties;

namespace TelnetTS.MVVM.Model
{
    public enum Switches
    {
        None = 0,
        Zyxel3500 = 1,
        Zyxel5000 = 2,
        Zyxel1000 = 3,
        Huawei5800 = 4,
        Huawei5600 = 5,
    }
    public partial class ConnectPoint : BaseModel
    {
        private string User = Settings.Default.UserName;
        private string Pass = Settings.Default.Password;

        public Switches TypeSwitch
        {
            get => typeSwitch;
            set
            {
                typeSwitch = value;
                OnPropertyChanged();
            }
        }
        private Switches typeSwitch = Switches.None;
        public string Port
        {
            get => port;
            set
            {
                port = value;
                OnPropertyChanged();
            }
        }
        private string port = "-";
        private string Mac
        {
            get => mac;
            set
            {
                var val = value;
                if (TypeSwitch == Switches.Huawei5600 || TypeSwitch == Switches.Huawei5800)
                    val = string.Join("-", ResponseReader.SplitByLength(val.Replace(":", string.Empty), 4));
                mac = val.Remove(val.Length - 2);
            }
        }
        private string mac;

        private bool IsAuth { get; set; } = false;
        //Preparation
        public ConnectPoint()
        {
            if (TClient != null && TClient.IsConneted)
                TClient.Disconnect();
        }
        public ConnectPoint(string connectionStr, string mac)
        {
            try
            {
                if (!string.IsNullOrEmpty(connectionStr))
                {
                    Determinant(connectionStr);
                    Mac = mac;

                    IsSuccess = !string.IsNullOrEmpty(IP) && IP != "-" && !string.IsNullOrEmpty(Port) && Port != "-";

                    if (!IsSuccess)
                        LogsAdd("Не удалось считать данные ПМ.");
                    if (IP != "-")
                        PingSwitch();
                    if (TypeSwitch == Switches.Huawei5800 || TypeSwitch == Switches.Huawei5600)
                        User += "@admin";
                }
                else
                    LogsAdd("Нет данных ПМ.");
            }
            catch
            {
                IsSuccess = false;
            }
        }
        private void Determinant(string input)
        {
            var match = Regex.Match(input, @"\d{1,4}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            if (!match.Success)
                return;

            var id = match.Value.IndexOf('.');
            IP = "10" + match.Value.Substring(id, match.Value.Length - id);

            //MetroEthernet
            if (input.Contains("FTTH") || input.Contains("ETTH") || input.Contains("MSS"))
            {
                TypeSwitch = Switches.Zyxel3500;
                string[] data = input.Split('/');
                Port = data.Count() > 2 ? data[2] : data[1].Substring(0, 2);
            }
            //DSL 5000/1000
            else if (input.Contains("atm"))
            {
                int i = input.IndexOf('m') + 2;
                int j = input.IndexOf(':');
                var port = input.Substring(i, j - i);

                if (input.Contains("560") || input.ToCharArray().Count(c => c == '/') > 1)
                {
                    TypeSwitch = Switches.Huawei5600;
                    Port = port.Replace("/0", string.Empty);
                }
                else if (port.Substring(0, port.IndexOf('/')) == "0")
                {
                    TypeSwitch = Switches.Zyxel1000;
                    Port = port.Substring(port.IndexOf("/") + 1);
                }
                else
                {
                    TypeSwitch = Switches.Zyxel5000;
                    Port = port.Replace("/", "-");
                }
            }
            //GPON
            else if (input.Contains("ont") || Regex.Matches(input, @"[a-zA-Z]").Count == 0)
            {
                TypeSwitch = Switches.Huawei5800;
                string[] strings = input.Split(' ');
                var port = strings[1];
                Port = strings[2] == "ont" ? $"{port} {strings[3]}" : $"{port} {strings[2]}";
            }
        }
        private async void PingSwitch()
        {
            int packets = 10;
            int success = 0;
            long max = 0;
            long min = 1000;
            long avg = 0;
            await Task.Run(() =>
            {
                for (int i = 0; i < packets; i++)
                {
                    Ping p = new Ping();
                    var ping = p.Send(IP, 1000);
                    if (ping.Status == IPStatus.Success)
                    {
                        var ms = ping.RoundtripTime;
                        success++;
                        max = ms > max ? ms : max;
                        min = ms < min ? ms : min;
                        avg += ms;
                    }
                }
            });
            bool imp = success != packets || (avg / packets) > 50;
            LogsAdd($"Пинги {success}/{packets}; min: {min}ms, max: {max}ms, avg: {avg / packets}ms.", imp);
            if (imp && TypeSwitch != Switches.Huawei5800)
                await CheckIncident();
        }
        private async Task CheckIncident()
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Clear();
                var res = await httpClient.GetStringAsync("http://192.168.114.142:3000/api/get_incidents?isGou=false&fix=true&mob=true&type=open&zues=%D0%92%D1%81%D0%B5+%D0%97%D0%A3%D0%AD%D0%A1&rues=%D0%92%D1%81%D0%B5+%D0%A0%D0%A3%D0%AD%D0%A1");

                if (res.Contains(IP.Remove(0, 3)))
                    LogsAdd("Обнаружен инцидент.");
            }
            catch
            {
                LogsAdd("Произошла ошибка при проверке инцидента.", false);
            }
        }

        //Launch
        public async void Main()
        {
            if (IsSuccess && !string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Pass))
            {
                await Connection();
                if (IsAuth)
                {
                    await CheckRequest();
                    SetTimer(min: 3);
                }
            }
            else if (string.IsNullOrEmpty(User) && string.IsNullOrEmpty(Pass))
                LogsAdd("Нет учетных данных в настройках.");
        }
        //Connection
        private async Task Connection()
        {
            try
            {
                IsBusy = true;

                var attemptLogin = 2;
                while (attemptLogin != 0)
                {                                   
                    await TClient.ConnectionAsync(IP, User, Pass);
                    await IsAuthorized();
                    if (IsAuth)
                        break;
                    attemptLogin--;
                    LogsAdd("Дополнительная попытка авторизации.", false);
                }
                if (attemptLogin == 0)
                {
                    LogsAdd("Не удалось авторизоваться.");
                    Disconnect();
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                    LogsAdd("Оборудование недоступен.");
            }
            catch
            {
                LogsAdd("Произошла ошибка при подключение к оборудованию.");
            }
            finally
            {
                IsBusy = false;
            }
        }
        private async Task IsAuthorized()
        {
            IsAuth = true;
            bool needEn = false;
            int pass = 0;
            while (true)
            {
                var line = await TClient.ReadAsync();
                TelnetResponse += line;
                if (line == null) break;

                if (TypeSwitch == Switches.Zyxel3500 || TypeSwitch == Switches.Zyxel5000 || TypeSwitch == Switches.Zyxel1000)
                {
                    if (line.Contains(">") && TypeSwitch == Switches.Zyxel3500)
                        needEn = true;
                    if (line.Contains("Password"))
                    {
                        pass++;
                        if (pass > 1)
                        {
                            IsAuth = false;
                            User = "admin";
                            Pass = "1234";
                            break;
                        }
                    }
                }
                else if (TypeSwitch == Switches.Huawei5800 || TypeSwitch == Switches.Huawei5600)
                {
                    if (line.Contains("Username or password invalid"))
                    {
                        IsAuth = false;
                        User = "root";
                        Pass = "admin123";
                        break;
                    }
                }
            }
            if (IsAuth)
                await SwitchEnable(needEn);
            else 
                Disconnect();
        }
        private async Task SwitchEnable(bool needEn)
        {
            switch (TypeSwitch)
            {
                case Switches.Zyxel3500:
                    if (needEn)
                    {
                        await TClient.SendAsync("en");
                        await TClient.SendAsync("1234");
                    }
                    break;
                case Switches.Zyxel5000:
                    await TClient.SendAsync($"sys ch stdsh");
                    await TClient.SendAsync($"y");
                    await TClient.SendAsync("en 14");
                    await TClient.SendAsync("1234");
                    break;
                case Switches.Huawei5600:
                    await TClient.SendAsync("en");
                    break;
                case Switches.Huawei5800:
                    await TClient.SendAsync("en");
                    break;
            }
        }
        //Check
        private async Task CheckRequest()
        {
            try
            {
                IsBusy = true;

                await CheckLink();
                if (IsActive)
                    await CheckMac();
                await CheckPar();
            }
            catch
            {
                LogsAdd("Произошла ошибка при проверке порта.");
            }
            finally
            {
                IsBusy = false;
            }
        }
        private async Task CheckLink()
        {
            //send command
            switch(TypeSwitch)
            {
                case Switches.Zyxel3500:
                    await TClient.SendAsync($"show interfaces {Port}", "c");
                    await TClient.SendAsync("\r\n");
                    break;
                case Switches.Zyxel5000:
                    await TClient.SendAsync($"show linestat {Port}");
                    break;
                case Switches.Zyxel1000:
                    await TClient.SendAsync($"statistics adsl show {Port}");
                    break;
                case Switches.Huawei5800:
                    await TClient.SendAsync($"display ont info {Port.Replace("/", " ")}", "\r\n");
                    await TClient.SendAsync("c", "q");
                    break;
                case Switches.Huawei5600:
                    await TClient.SendAsync($"display line operation port {Port}", "y");
                    break;
            }

            bool gponCopy = false;
            await TClient.SendAsync("\r\n");
            while (true)
            {
                var line = await TClient.ReadAsync();
                if (line == null) break;
                TelnetResponse += line;

                switch (TypeSwitch)
                {
                    case Switches.Zyxel3500:
                        {
                            if (line.Contains("Port"))
                                Copy.AppendLine(line);
                            if (line.Contains("Link"))
                            {
                                if (line.Contains(":10"))
                                    IsActive = true;
                                Copy.AppendLine(line);
                            }
                            if (line.Contains("Errors"))
                                Copy.AppendLine(line);
                            if (line.Contains("Up Time") && IsActive)
                            {
                                UpTime = TimeSpanParser.Main(line);
                                Copy.AppendLine(line);
                                Copy.AppendLine("\r");
                            }
                            break;
                        }
                    case Switches.Zyxel5000:
                        {
                            string[] str = ResponseReader.SplitLine(line);
                            if (str.Length > 2 && (str[1].Contains("up") || str[2].Contains("up")))
                            {
                                IsActive = true;
                                string time = string.Empty;
                                var id = Array.IndexOf(str, str.FirstOrDefault(x => x.Contains("adsl") || x.Contains("gdmt")));
                                for (int i = id + 1; i < str.Length; i++)
                                    time += str[i];
                                UpTime = TimeSpanParser.Main(time, TypeSwitch);
                            }
                            break;
                        }
                    case Switches.Zyxel1000:
                        {
                            string[] str = ResponseReader.SplitLine(line);
                            if (str.Length > 2 && str[0] == Port && str[1] == "V")
                            {
                                IsActive = true;
                                UpTime = TimeSpanParser.Main(str[str.Length - 2]);
                            }
                            break;
                        }
                    case Switches.Huawei5800:
                        {
                            if (line.Contains("F/S/P") || gponCopy)
                            {
                                gponCopy = true;
                                Copy.AppendLine(line);
                                if (line.Contains("Run state"))
                                {
                                    if (line.Contains("online"))
                                        IsActive = true;
                                    gponCopy = false;
                                }
                            }
                            if (line.Contains("Last down cause"))
                                Copy.AppendLine(line);
                            if (line.Contains("ONT online duration") && IsActive)
                            {
                                UpTime = TimeSpanParser.Main(line);
                                Copy.AppendLine(line);
                            }
                            break;
                        }
                    case Switches.Huawei5600:
                        {
                            if (line.Contains("Downstream"))
                                IsActive = true;
                            break;
                        }
                }
            }
        }
        private async Task CheckMac()
        {
            //send command
            switch (TypeSwitch)
            {
                case Switches.Zyxel3500:
                    await TClient.SendAsync($"show mac address-table port {Port}");
                    break;
                case Switches.Zyxel5000:
                    await TClient.SendAsync($"show mac {Port}");
                    break;
                case Switches.Zyxel1000:
                    await TClient.SendAsync($"statistics mac {Port}");
                    break;
                case Switches.Huawei5800:
                    string[] ports = Port.Split(' ');
                    await TClient.SendAsync($"display mac-address port {ports[0]} ont {ports[1]}", "\r\n");
                    break;
                case Switches.Huawei5600:
                    await TClient.SendAsync($"display mac-address adsl {Port}", "y");
                    await TClient.SendAsync($"display mac-address port {Port}", "y");
                    break;
            }
            await TClient.SendAsync("\r\n");

            //check response
            bool isMac = false;
            bool isMatch = false;
            while (true)
            {
                var line = await TClient.ReadAsync();
                if (line == null) break;
                TelnetResponse += line;

                var huawei = TypeSwitch == Switches.Huawei5800 || TypeSwitch == Switches.Huawei5600;
                var macLine = ResponseReader.GetMacAddressFromString(line, huawei);
                if (!string.IsNullOrEmpty(macLine))
                {
                    isMac = true;
                    if (macLine.Contains(Mac))
                        isMatch = true;
                }
            }
            if (!isMac)
            {
                LogsAdd("Мак отсуствует.");
                if (TypeSwitch == Switches.Huawei5800)
                    await CheckLinkToRouterGpon();
            }
            else if (isMac && !isMatch)
                LogsAdd("Мак есть, но не совпадает.");
        }
        private async Task CheckLinkToRouterGpon()
        {
            var port = Port.Replace(" ", "/");
            string[] ports = port.Split('/');

            await TClient.SendAsync("config");
            await TClient.SendAsync($"interface gpon {ports[0]}/{ports[1]}");
            await TClient.SendAsync($"display ont port state {ports[2]} {ports[3]} eth-port all", "\r\n");
            await TClient.SendAsync("quit");
            await TClient.SendAsync("quit");

            while (true)
            {
                var line = await TClient.ReadAsync();
                TelnetResponse += line;
                if (line == null) break;

                string[] str = ResponseReader.SplitLine(line);
                if (str[0] == ports[3])
                {
                    if (str[5].Contains("up"))
                        LogsAdd("Сигнал до роутера есть.");
                    else
                        LogsAdd("Отсуствует сигнал до роутера.");
                }
            }
        }
        private async Task CheckPar()
        {
            string[] ports = Port.Split(' ');
            //send command
            switch (TypeSwitch)
            {
                case Switches.Zyxel3500:
                    await TClient.SendAsync($"cable-diagnostics {Port}");
                    await Task.Delay(3000);
                    break;
                case Switches.Zyxel5000:
                    await TClient.SendAsync($"show linerate {Port}");
                    break;
                case Switches.Zyxel1000:
                    await TClient.SendAsync($"statistics adsl linerate {Port}");
                    break;
                case Switches.Huawei5800:
                    await TClient.SendAsync($"display ont info summary {ports[0]}", "\r\n");
                    for (int i = 0; i < 10; i++)
                        await TClient.SendAsync("c");
                    break;
                case Switches.Huawei5600:
                    await TClient.SendAsync($"display line operation port {Port}", "y");
                    break;
            }
            await TClient.SendAsync("\r");

            //check response
            bool isBad = false;
            while (true)
            {
                var line = await TClient.ReadAsync();
                if (line == null) break;
                TelnetResponse += line;

                if (!line.Contains("#") && !line.Contains(">") && !line.Contains("(y/n)") && !line.Contains("take several") && TypeSwitch != Switches.Huawei5800)
                    Copy.AppendLine(line);
                switch (TypeSwitch)
                {
                    case Switches.Zyxel3500:
                        if (line.Contains("pairA") || line.Contains("pairB"))
                            if (!line.Contains("Ok"))
                                isBad = true;
                        break;
                    case Switches.Zyxel5000:
                        if (IsActive && (line.Contains("margin") || line.Contains("attenuation")))
                        {
                            string[] str = ResponseReader.SplitLine(line);
                            var up = ResponseReader.GetDouble(str[str.Length - 2]);
                            var down = ResponseReader.GetDouble(str[str.Length - 1]);
                            if (line.Contains("margin"))
                            {
                                if (up < 10 || down < 10)
                                    isBad = true;
                            }
                            else
                            {
                                if (up > 30 || down > 30)
                                    isBad = true;
                            }
                        }
                        break;
                    case Switches.Zyxel1000:
                        if (IsActive && (line.Contains("margin") || line.Contains("attenuation")))
                        {
                            string[] str = ResponseReader.SplitLine(line);
                            string[] par = str.LastOrDefault().Split('/');
                            var up = ResponseReader.GetDouble(par[0]);
                            var down = ResponseReader.GetDouble(par[1]);
                            if (line.Contains("margin"))
                            {
                                if (up < 10 || down < 10)
                                    isBad = true;
                            }
                            else
                            {
                                if (up > 30 || down > 30)
                                    isBad = true;
                            }
                        }
                        break;
                    case Switches.Huawei5800:
                        string[] split = line.Split(' ');
                        if (split[0] == ports[1])
                        {
                            Copy.AppendLine(line);
                            if (IsActive)
                            {
                                string[] str = ResponseReader.SplitLine(line);
                                string[] par = str[4].Split('/');
                                var up = ResponseReader.GetDouble(par[0]);

                                if (up <= -27)
                                    isBad = true;
                            }
                        }
                        break;
                    case Switches.Huawei5600:
                        if (IsActive && (line.Contains("margin") || line.Contains("attenuation")))
                        {
                            string[] str = ResponseReader.SplitLine(line);
                            var par = ResponseReader.GetDouble(str.LastOrDefault());
                            if (line.Contains("margin"))
                            {
                                if (par < 10)
                                    isBad = true;
                            }
                            else
                            {
                                if (par > 30)
                                    isBad = true;
                            }
                        }
                        break;
                }
            }
            if (isBad)
                LogsAdd("Параметры линии плохие.");
        }

        public ICommand CopyCommand =>
            new RelayCommand((obj) =>
            {
                Clipboard.SetText(Copy.ToString());
            }, (obj) => !IsBusy && !string.IsNullOrEmpty(Copy.ToString()));
        public ICommand RefreshRequestCommand =>
            new RelayCommand(async (obj) =>
            {
                Copy.Clear();
                IsActive = false;
                UpTime = new TimeSpan();
                LogsClear();

                PingSwitch();
                await CheckRequest();
                AddTimeToTimer(min: 3);

            }, (obj) => !IsBusy && IsSuccess && TClient.IsConneted);
        public ICommand RebootRequestCommand =>
            new RelayCommand(async (obj) =>
            {
                IsActive = false;
                UpTime = new TimeSpan();
                await RebootPort();
                LogsAdd("Порт был перезапущен.", false);
                AddTimeToTimer(min: 3);

            }, (obj) => !IsBusy && IsSuccess && TClient.IsConneted);
        private async Task RebootPort()
        {
            try
            {
                IsBusy = true;
                switch (TypeSwitch)
                {
                    case Switches.Zyxel3500:
                        await TClient.SendAsync("configure");
                        await TClient.SendAsync($"interface port-channel {Port}");
                        await TClient.SendAsync("inactive");
                        await TClient.SendAsync("no inactive");
                        await TClient.SendAsync("exit");
                        await TClient.SendAsync("exit");
                        break;
                    case Switches.Zyxel1000:
                        await TClient.SendAsync($"adsl disable {Port}");
                        await TClient.SendAsync($"adsl enable {Port}");
                        break;
                    case Switches.Zyxel5000:
                        await TClient.SendAsync($"port disable {Port}");
                        await TClient.SendAsync($"port enable {Port}");
                        break;
                    case Switches.Huawei5800:
                        var port = Port.Replace(" ", "/");
                        string[] ports = port.Split('/');
                        await TClient.SendAsync("config");
                        await TClient.SendAsync($"interface gpon {ports[0]}/{ports[1]}");
                        await TClient.SendAsync($"ont reset {ports[2]} {ports[3]}", "y");
                        await TClient.SendAsync("quit");
                        await TClient.SendAsync("quit");
                        break;
                    case Switches.Huawei5600:
                        break;
                }
            }
            catch
            {
                LogsAdd("Произошла ошибка при перезапуске порта.");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
