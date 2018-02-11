using System;
using System.Linq;

namespace Insight.Metrics
{
    internal static class Statistics
    {
        public static double Mean(int[] data)
        {
            return (double) data.Sum() / data.Length;
        }

        public static double StandardDeviation(int[] data)
        {
            return Math.Sqrt(Variance(data));
        }

        public static double Variance(int[] data)
        {
            var denominator = data.Length;
            var mean = Mean(data);

            var sumSquaredResiduals = 0.0;
            for (var i = 0; i < data.Length; ++i)
            {
                sumSquaredResiduals += Math.Pow(data[i] - mean, 2);
            }

            return sumSquaredResiduals / denominator;
        }
    }
}