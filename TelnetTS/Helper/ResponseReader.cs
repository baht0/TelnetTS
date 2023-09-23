using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TelnetTS.Helper
{
    internal class ResponseReader
    {
        public static string[] SplitLine(string input)
        {
            var result = Regex.Replace(input, @"\s+", " ").Trim();
            return result.Replace("/ ", "/").Split(' ');
        }
        public static double GetDouble(string input)
        {
            double.TryParse(input, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out double res);
            return res;
        }
        public static string GetMacAddressFromString(string input, bool isAlternative)
        {
            string pattern = @"\b([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})\b";//zyxel
            if (isAlternative)
                pattern = @"\b([0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4})\b";//huawei

            MatchCollection matches = Regex.Matches(input, pattern);

            return matches.Count > 0 ? matches[0].Value : string.Empty;
        }
        public static string[] SplitByLength(string mac, int length)
        {
            int numSegments = (mac.Length + length - 1) / length;
            string[] segments = new string[numSegments];
            for (int i = 0; i < numSegments; i++)
            {
                int startIndex = i * length;
                int segmentLength = Math.Min(length, mac.Length - startIndex);
                segments[i] = mac.Substring(startIndex, segmentLength);
            }
            return segments;
        }
    }
}
