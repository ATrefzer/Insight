using System.Collections.Generic;

using Insight.Shared.Model;

using Visualization.Controls.Data;

namespace Insight.Builder
{
    public sealed class HotspotBuilder : HierarchyBuilder
    {
        private Dictionary<string, LinesOfCode> _metrics;

        public HierarchicalData Build(List<Artifact> reduced, Dictionary<string, LinesOfCode> metrics)
        {
            _metrics = metrics;
            return Build(reduced);
        }

        protected override double GetArea(Artifact item)
        {
            var area = 0.0;
            var key = item.LocalPath.ToLowerInvariant();

            if (_metrics.ContainsKey(key))
            {
                // Lines of code
                area = _metrics[key].Code;
            }

            return area;
        }

        protected override string GetDescription(Artifact item)
        {
            return item.ServerPath + "\nCommits: " + item.Commits + "\nLOC: " + GetArea(item);
        }

        protected override double GetWeight(Artifact item)
        {
            var weight = item.Commits;
            return weight;
        }

        protected override bool IsAccepted(Artifact item)
        {
            // Area must > 0 because of division.
            var area = GetArea(item);
            var weight = GetWeight(item);

            // File must have a size (lines of code) and must have been at least 2 times committed.
            return area > 0 && weight > 2;
        }
    }
}