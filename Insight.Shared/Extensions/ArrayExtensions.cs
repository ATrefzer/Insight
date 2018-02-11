using System;

namespace Insight.Shared.Extensions
{
    public static class ArrayExtensions
    {
        // create a subset from a specific list of indices
        public static T[] Subset<T>(this T[] oldArray, int from)
        {
            var newArray = new T[oldArray.Length - from];
            Array.Copy(oldArray, from, newArray, 0, newArray.Length);
            return newArray;
        }
    }
}