using System;

namespace Insight.Shared.Model
{
    public sealed class TrendData
    {
        public DateTime Date { get; set; }

        public InvertedSpace InvertedSpace { get; set; }

        public LinesOfCode Loc { get; set; }
    }
}