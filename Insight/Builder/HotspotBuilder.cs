using System.Collections.Generic;
using System.Globalization;
using Insight.Analyzers;
using Insight.Metrics;
using Insight.Shared;
using Insight.Shared.Model;

using Visualization.Controls.Interfaces;

namespace Insight.Builder
{
    public sealed class HotspotBuilder : HierarchyBuilder
    {
        private HotspotCalculator _hotspotCalculator;

        public IHierarchicalData Build(List<Artifact> artifacts, Dictionary<string, LinesOfCode> metrics)
        {
            _hotspotCalculator = new HotspotCalculator(artifacts, metrics);
         
            return Build(artifacts);
        }

        protected override double GetArea(Artifact item)
        {
            return _hotspotCalculator.GetLinesOfCode(item);
        }

        protected override string GetDescription(Artifact item)
        {
            var hotspot = _hotspotCalculator.GetHotspotValue(item);
            return item.ServerPath 
                + "\nCommits: " 
                + item.Commits + "\nLOC: " 
                + _hotspotCalculator.GetLinesOfCode(item) + "\nHotspot: " 
                + hotspot.ToString("F5", CultureInfo.InvariantCulture);
        }

        protected override double GetWeight(Artifact item)
        {
            return _hotspotCalculator.GetCommits(item);
        }

        protected override bool IsAccepted(Artifact item)
        {
            // Area must > 0 because of division.
            var area = GetArea(item);
            var weight = GetWeight(item);

            // File must have a size (lines of code) and must have been at least 2 times committed.
            return area > Thresholds.MinLinesOfCodeForHotspot && weight > Thresholds.MinCommitsForHotspots;
        }
    }
}