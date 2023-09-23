using System;
using System.Globalization;
using System.Windows.Data;
using TelnetTS.MVVM.Model;

namespace TelnetTS.Converters
{
    internal class SwitchToConnectionTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((Switches)value)
            {
                case Switches.Zyxel3500:
                    return "Ethernet";
                case Switches.Huawei5800:
                    return "GPON";
                case Switches.Zyxel5000:
                    return "ADSL";
                case Switches.Zyxel1000:
                    return "ADSL";
                case Switches.Huawei5600:
                    return "ADSL";
                default:
                    return "-";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((Switches)value)
            {
                case Switches.Zyxel3500:
                    return "Ethernet";
                case Switches.Huawei5800:
                    return "GPON";
                case Switches.Zyxel5000:
                    return "ADSL";
                case Switches.Zyxel1000:
                    return "ADSL";
                case Switches.Huawei5600:
                    return "ADSL";
                default:
                    return "-";
            }
        }
    }
}
