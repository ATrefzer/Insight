using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Insight.GitProvider
{
    public static class Decoder
    {
        static readonly Regex _regex = new Regex(@"(?<Value>(\\[a-zA-Z0-9]{3})+)", RegexOptions.Compiled);

        //public static string DeodeUtf8(string value)
        //{
        //    return Encoding.UTF8.GetString(bytes)
        //}
        // TODO that seems unreliable

        /// <summary>
        /// Decodes Utf8 escape sequences
        /// In git path names are encoded like  "äöü" -> "\303\244\303\266\303\274"
        /// Note that the numbers are octal!
        /// </summary>
        public static string Decode(string escapedString)
        {
            if (escapedString == null)
            {
                // Can happen at the end of the file. Be robust.
                return null;
            }

            //var bytes = Encoding.GetEncoding(1252).GetBytes(value);
            //var fixedValue = Encoding.UTF8.GetString(bytes);

            //string source = @"\354\202\254\354\232\251\354\236\220\354\203\201\354\204" +
            //                @"\270\354\240\225\353\263\264\354\236\205\353\240\245";

            

            //string result = Encoding.UTF8.GetString(bytes);   // "사용자상세정보입력"

            try
            {
                var replace = _regex.Replace(
                                             escapedString,
                                             m =>
                                             {
                                                 var escaped = m.Groups["Value"].Value;
                                                 byte[] bytes = escaped.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries)
                                                                      .Select(s => (byte)Convert.ToInt32(s, 8))
                                                                      .ToArray();
                                                 var decoded = Encoding.UTF8.GetString(bytes);
                                                 return decoded;
                                             });
                return replace.Trim('"');
            }
            catch (Exception ex)
            {
                return escapedString;
            }
        }
    }
}