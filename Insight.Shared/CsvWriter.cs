using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Insight.Shared
{
    /// <summary>
    /// Simplistic version of a csv serializer.
    /// </summary>
    public class CsvWriter
    {
        /// <summary>
        /// Examples
        /// https://msdn.microsoft.com/de-de/library/kfsatb94(v=vs.110).aspx
        /// E3  1.054E+003, use e for lower case.
        /// F3  1054.322
        /// </summary>
        public string NumberFormat { get; set; } = "F3";
        public bool Header { get; set; } = false;

        public string ToCsv<T>(IEnumerable<T> items)
        {
            if (items == null || !items.Any())
            {
                return "";
            }
            StringBuilder builder = new StringBuilder();
            Process(items, line => builder.AppendLine(line));
            return builder.ToString();
        }

        public void Process<T>(IEnumerable<T> items, Action<string> writeLine)
        {
            var type = typeof(T);

            var propertyInfos = type.GetProperties();
            var names = propertyInfos.Select(pi => pi.Name).ToList();

            WriteHeader(propertyInfos, writeLine);
            WriteItems(propertyInfos, writeLine, items);
        }

        private void WriteItems<T>(PropertyInfo[] propertyInfos, Action<string> writeLine, IEnumerable<T> items)
        {
            if (items == null || !items.Any())
            {
                return;
            }

            string numberFormat = "{0:" + NumberFormat + "}";
            var line = new List<string>(propertyInfos.Length);
            foreach (var item in items)
            {
                line.Clear();
                foreach (var propertyInfo in propertyInfos)
                {
                    var name = propertyInfo.Name;
                    var value = propertyInfo.GetValue(item);

                    if (IsNumberFormat(propertyInfo))
                    {
                        line.Add(string.Format(CultureInfo.InvariantCulture, numberFormat, value));
                    }
                    else
                    {
                        var str = value.ToString();
                        if (str.Any(c => c == ',' || c == ' ' || c == '\t'))
                        {
                            str = "\"" + str + "\"";
                        }

                        line.Add(str);
                    }
                }

                writeLine(string.Join(",", line));
            }
        }

        private void WriteHeader(PropertyInfo[] propertyInfos, Action<string> writeLine)
        {
            if (Header)
            {
                List<string> header = new List<string>();
                foreach (var propertyInfo in propertyInfos)
                {
                    header.Add(propertyInfo.Name);

                }
                writeLine(string.Join(",", header));
            }
        }

        private bool IsNumberFormat(PropertyInfo propertyInfo)
        {
            return (propertyInfo.PropertyType == typeof(double) ||
                propertyInfo.PropertyType == typeof(float)) &&
                !string.IsNullOrEmpty(NumberFormat);
        }

        public void ToCsv<T>(string filePath, IEnumerable<T> items)
        {
            using (var stream = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                Process(items, line => stream.WriteLine(line));
            }
        }
    }
}
