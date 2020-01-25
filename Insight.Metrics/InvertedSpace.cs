namespace Insight.Metrics
{
    /// <summary>
    /// Inverted space metric for a single file.
    /// </summary>
    public sealed class InvertedSpace
    {
        public int Max { get; set; }
        public double Mean { get; set; }
        public int Min { get; set; }
        public double StandardDeviation { get; set; }
        public int Total { get; set; }
    }
}