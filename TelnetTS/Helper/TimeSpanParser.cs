using System;
using System.Linq;
using System.Text.RegularExpressions;
using TelnetTS.MVVM.Model;

namespace TelnetTS.Helper
{
    public class TimeSpanParser
    {
        public static TimeSpan Main(string input, Switches switches = Switches.None)
        {
            if (input.Any(char.IsLetter))
                input = ReplaceNonDigitsWithColon(input);
            return ParseTimeSpan(input, switches);
        }
        static TimeSpan ParseTimeSpan(string timeSpanString, Switches switches)
        {
            string[] timeComponents = timeSpanString.Split(':');

            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            switch (timeComponents.Length)
            {
                case 1:
                    if (switches == Switches.Zyxel5000)
                        seconds = int.Parse(timeComponents[0]);
                    break;
                case 2:
                    if (switches == Switches.Zyxel5000)
                    {
                        minutes = int.Parse(timeComponents[0]);
                        seconds = int.Parse(timeComponents[1]);
                    }
                    else if (switches == Switches.None)
                    {
                        days = int.Parse(timeComponents[0]);
                        hours = int.Parse(timeComponents[1]);
                    }
                    break;
                case 3:
                    hours = int.Parse(timeComponents[0]);
                    minutes = int.Parse(timeComponents[1]);
                    seconds = int.Parse(timeComponents[2]);
                    break;
                case 4:
                    days = int.Parse(timeComponents[0]);
                    hours = int.Parse(timeComponents[1]);
                    minutes = int.Parse(timeComponents[2]);
                    seconds = int.Parse(timeComponents[3]);
                    break;
            }
            return new TimeSpan(days, hours, minutes, seconds);
        }
        static string ReplaceNonDigitsWithColon(string input)
        {
            string result = Regex.Replace(input, @"\D", ":");
            result = Regex.Replace(result, ":+", ":");
            return result.Trim(':');
        }
    }
}
