using System;
using System.Globalization;
using System.Windows.Data;

namespace TelnetTS.Converters
{
    public class TimeSpanToReadableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan span = (TimeSpan)value;
            string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0}d. ", span.Days) : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0}h. ", span.Hours) : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0}m. ", span.Minutes) : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0}s.", span.Seconds) : string.Empty);
            return !string.IsNullOrEmpty(formatted) ? formatted : "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan span = (TimeSpan)value;
            string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0}d. ", span.Days) : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0}h. ", span.Hours) : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0}m. ", span.Minutes) : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0}s.", span.Seconds) : string.Empty);
            return !string.IsNullOrEmpty(formatted) ? formatted : "-";
        }
    }
}
