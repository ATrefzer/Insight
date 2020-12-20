using Insight.Metrics;
using Insight.Shared.Model;
using System;
using System.Collections.Generic;

namespace Insight.Analyzers
{
    public sealed class HotspotCalculator
    {
        readonly double _maxCommits = double.MinValue;
        readonly double _minCommits = double.MaxValue;
        readonly double _minLinesOfCode = double.MaxValue;
        readonly double _maxLinesOfCode = double.MinValue;
        readonly Dictionary<string, LinesOfCode> _metrics;

        public HotspotCalculator(IEnumerable<Artifact> artifacts, Dictionary<string, LinesOfCode> metrics)
        {
            _metrics = metrics;
            foreach (var artifact in artifacts)
            {
                _maxCommits = Math.Max(_maxCommits, GetCommits(artifact));
                _minCommits = Math.Min(_minCommits, GetCommits(artifact));
                _maxLinesOfCode = Math.Max(_maxLinesOfCode, GetLinesOfCode(artifact));
                _minLinesOfCode = Math.Min(_minLinesOfCode, GetLinesOfCode(artifact));
            }
        }

        /// <summary>
        /// Lines of Code
        /// </summary>
        public double GetLinesOfCode(Artifact item)
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
        public double GetCommits(Artifact item)
        {
            var weight = item.Commits;
            return weight;
        }

        public double GetHotspotValue(Artifact item)
        {
            // Calculate hotspot index
            var normalizedWeight = (GetCommits(item) - _minCommits) / (_maxCommits - _minCommits);
            var normalizedArea = (GetLinesOfCode(item) - _minLinesOfCode) / (_maxLinesOfCode - _minLinesOfCode);
            var hotspot = normalizedWeight * normalizedArea;
            return hotspot;
        }
    }
}
