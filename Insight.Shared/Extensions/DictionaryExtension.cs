using System.Collections.Generic;

namespace Insight.Shared.Extensions
{
    /// <summary>
    ///     Accumulate work stored in a dictionary value.
    /// </summary>
    public static class DictionaryExtension
    {
        public static void AddToValue<TKey>(
                this Dictionary<TKey, int> dict, TKey key, int work)
        {
            if (dict.TryGetValue(key, out var currentValue))
            {
                dict[key] = currentValue + work;
            }
            else
            {
                dict[key] = work;
            }
        }
    }
}