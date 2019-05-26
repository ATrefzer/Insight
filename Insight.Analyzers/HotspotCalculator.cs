using Insight.Metrics;
using Insight.Shared.Model;
using System;
using System.Collections.Generic;

namespace Insight.Analyzers
{
    public class HotspotCalculator
    {
        double _weightMax = double.MinValue;
        double _weightMin = double.MaxValue;
        double _areaMin = double.MaxValue;
        double _areaMax = double.MinValue;
        Dictionary<string, LinesOfCode> _metrics;

        public HotspotCalculator(List<Artifact> artifacts, Dictionary<string, LinesOfCode> metrics)
        {
            _metrics = metrics;
            foreach (var artifact in artifacts)
            {
                _weightMax = Math.Max(_weightMax, GetWeight(artifact));
                _weightMin = Math.Min(_weightMin, GetWeight(artifact));
                _areaMax = Math.Max(_areaMax, GetArea(artifact));
                _areaMin = Math.Min(_areaMin, GetArea(artifact));
            }
        }

        /// <summary>
        /// Lines of Code
        /// </summary>
        public double GetArea(Artifact item)
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

        /// <summary>
        /// Commits
        /// </summary>
        public double GetWeight(Artifact item)
        {
            var weight = item.Commits;
            return weight;
        }

        public double GetHotspot(Artifact item)
        {
            // Calculate hotspot index
            double hotspot = 0.0;
            var normalizedWeight = (GetWeight(item) - _weightMin) / (_weightMax - _weightMin);
            var normalizedArea = (GetArea(item) - _areaMin) / (_areaMax - _areaMin);
            hotspot = normalizedWeight * normalizedArea;
            return hotspot;
        }
    }
}
