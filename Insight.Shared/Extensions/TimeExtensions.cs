using System;

namespace Insight.Shared.Extensions
{
    public static class TimeExtensions
    {
        public static TimeSpan Days(this int days)
        {
            return TimeSpan.FromDays(days);
        }

        public static string ToIsoShort(this DateTime date)
        {
            return string.Format(date.ToString("yyyy-MM-dd"));
        }
    }
}