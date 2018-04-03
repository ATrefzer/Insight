﻿using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Insight.GitProvider
{
    public static class Decoder
    {
        static readonly Regex _regex = new Regex(@"(?<Value>(\\[a-zA-Z0-9]{3})+)", RegexOptions.Compiled);

        /// <summary>
        /// From git manual: "Path names are encoded in UTF-8 normalization form C"
        /// Decodes these escape sequences.
        /// Example: "äöü" -> "\303\244\303\266\303\274"
        /// Note that the numbers are octal!
        /// Based on
        /// https://stackoverflow.com/questions/24273673/i-have-a-string-of-octal-escapes-that-i-need-to-convert-to-korean-text-not-sur
        /// </summary>
        public static string DecodeEscapedBytes(string escapedString)
        {
            if (escapedString == null)
            {
                // Can happen at the end of the file. Be robust.
                return null;
            }

            try
            {
                var replace = _regex.Replace(escapedString,
                                             m =>
                                             {
                                                 var escaped = m.Groups["Value"].Value;
                                                 return UnescapeSequence(escaped);
                                             });
                return replace.Trim('"');
            }
            catch (Exception ex)
            {
                return escapedString;
            }
        }

        /// <summary>
        /// NOT USED FOR THE MOMENT. SET ENCODING FOR PROCESS STDOUT INSTEAD.
        /// From git manual: "Commit log messages are typically encoded in UTF-8, but other extended ASCII encodings are also
        /// supported"
        /// Same applies to autor name.
        /// </summary>
        public static string DecodeUtf8(string encoded)
        {
            if (encoded == null)
            {
                return null;
            }

            var bytes = Encoding.GetEncoding(1252).GetBytes(encoded);
            var decoded = Encoding.UTF8.GetString(bytes);

            //Debug.Assert(decoded == encoded);
            return decoded;
        }

        public static string UnescapeSequence(string escaped)
        {
            var bytes = escaped.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => (byte) Convert.ToInt32(s, 8))
                               .ToArray();
            var decoded = Encoding.UTF8.GetString(bytes);
            return decoded;
        }
    }
}