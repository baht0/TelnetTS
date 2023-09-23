using Microsoft.Win32;
using System.Windows.Input;
using TelnetInfo.Core;
using TelnetTS.Properties;

namespace TelnetTS.MVVM.ViewModel
{
    public partial class SettingViewModel : ObservableObject
    {
        public string UserName
        {
            get => userName;
            set
            {
                userName = value;
                OnPropertyChanged();
            }
        }
        private string userName = Settings.Default.UserName;
        public string Password
        {
            get => password;
            set
            {
                password = value;
                OnPropertyChanged();
            }
        }
        private string password = Settings.Default.Password;

        private string PathExe { get; set; } = Settings.Default.PathExe;
        public bool IsPathExe
        {
            get => isPathExe;
            set
            {
                isPathExe = value;
                OnPropertyChanged();
            }
        }
        private bool isPathExe = !string.IsNullOrEmpty(Settings.Default.PathExe);

        public ICommand PinSecureCRTCommand =>
            new RelayCommand((obj) =>
            {
                var exe = new OpenFileDialog
                {
                    Title = "Выберите испольняемый файл SecureCRT",
                    Filter = "securecrt.exe |securecrt.exe",
                    FilterIndex = 1,
                    Multiselect = false
                };
                PathExe = exe.ShowDialog() == true ? exe.FileName : PathExe;
                IsPathExe = !string.IsNullOrEmpty(PathExe);

                //var dialog = new SaveFileDialog
                //{
                //    Title = "Выберите папку с настройками SecureCRT",
                //    Filter = "Выберите папку|*.folder",
                //    FileName = "Config"
                //};
                //if (dialog.ShowDialog() == true)
                //{
                //    string path = dialog.FileName;
                //    PathConfig = path.Replace($"\\{dialog.SafeFileName}", string.Empty);
                //}
            });
        public ICommand SaveCommand =>
            new RelayCommand((obj) =>
            {
                Settings.Default.UserName = UserName;
                Settings.Default.Password = Password;
                Settings.Default.PathExe = PathExe;
                Settings.Default.Save();
            });
    }
}
