using System;
using System.Globalization;
using System.Windows.Data;

namespace TelnetTS.Converters
{
    public class IntToRateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (double)value != 0 ? Math.Round((double)value / 1000, 2) + " мбит/с" : "-";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (double)value != 0 ? Math.Round((double)value / 1000, 2) + " мбит/с" : "-";
    }
}
