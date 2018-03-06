using System;
using System.Collections.Generic;

using Insight.Shared.Model;

using Visualization.Controls.Data;

namespace Insight.Builder
{
    public sealed class CodeAgeBuilder : HierarchyBuilder
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
            return item.ServerPath + "\nDays since last commit: " + GetWeight(item);
        }

        protected override double GetWeight(Artifact item)
        {
            var weight = (DateTime.Now - item.Date).Days;
            return weight;
        }

        protected override bool IsAccepted(Artifact item)
        {
            // Area must > 0 because of division.
            var area = GetArea(item);

            // File must have a size (lines of code) 
            return area > 0;
        }
    }
}