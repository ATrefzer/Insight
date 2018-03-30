using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Insight.GitProvider
{
    static class Decoder
    {
        static readonly Regex _regex = new Regex(@"\\(?<Value>[a-zA-Z0-9]{3})", RegexOptions.Compiled);

        // TODO that seems unreliable
        public static string Decode(string value)
        {
            if (value == null)
            {
                return null;
            }

            var bytes = Encoding.GetEncoding(1252).GetBytes(value);
            var fixedValue = Encoding.UTF8.GetString(bytes);

            try
            {
                var replace = _regex.Replace(
                                             fixedValue,
                                             m => ((char) int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString()
                                            );
                return replace.Trim('"');
            }
            catch (Exception ex)
            {
                return fixedValue;
            }
        }
    }
}