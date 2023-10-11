using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Timers;
using TelnetInfo.Core;

namespace TelnetTS.MVVM.Model
{
    public class BaseModel : ObservableObject
    {
        public StringBuilder Copy = new StringBuilder();
        public bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged();
            }
        }
        private bool isBusy;
        public string TelnetResponse
        {
            get => telnetResponse;
            set
            {
                telnetResponse = value + "\r\n";
                OnPropertyChanged();
            }
        }
        private string telnetResponse = string.Empty;
        public ObservableCollection<Log> Logs
        {
            get => logs;
            set
            {
                logs = new ObservableCollection<Log>(value.Reverse());
                OnPropertyChanged();
            }
        }
        private ObservableCollection<Log> logs = new ObservableCollection<Log>();

        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                OnPropertyChanged();
            }
        }
        private bool isActive;
        public string IP
        {
            get => iP;
            set
            {
                iP = value;
                OnPropertyChanged();
            }
        }
        private string iP = "-";
        public TimeSpan UpTime
        {
            get
            {
                return upTime;
            }
            set
            {
                upTime = value;
                OnPropertyChanged();
            }
        }
        private TimeSpan upTime = new TimeSpan();

        protected bool IsSuccess { get; set; } = false;
        protected TelnetClient TClient { get; set; } = new TelnetClient();
        public void Disconnect()
        {
            if (TClient != null && TClient.IsConneted)
                TClient.Disconnect();
        }

        //TimeOut connection
        private DateTime DisconnectTime { get; set; }

        protected void SetTimer(int min)
        {
            DisconnectTime = DateTime.Now.AddMinutes(min);
            var timer = new Timer();
            timer.Elapsed += TimeOut;
            timer.Enabled = true;
        }
        protected void AddTimeToTimer(int min) => DisconnectTime = DateTime.Now.AddMinutes(min);
        private void TimeOut(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now >= DisconnectTime)
            {
                Disconnect();
                ((Timer)sender).Enabled = false;
                LogsAdd("Время подключение истекло.", false);
            }
        }

        //Logs
        private List<Log> InitialLogs { get; set; } = new List<Log>();
        protected void LogsAdd(string message, bool important = true)
        {
            var log = new Log()
            {
                Message = message,
                Important = important
            };
            InitialLogs.Add(log);
            Logs = new ObservableCollection<Log>(InitialLogs);
        }
        protected void LogsClear()
        {
            InitialLogs.Clear();
            Logs.Clear();
        }
    }
    public class Log
    {
        public string Message { get; set; }
        public bool Important { get; set; }
    }
}
