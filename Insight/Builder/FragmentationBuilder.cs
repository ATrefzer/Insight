using System.Collections.Generic;
using Insight.Metrics;
using Insight.Shared.Model;

using Visualization.Controls.Interfaces;

namespace Insight.Builder
{
    public sealed class FragmentationBuilder : HierarchyBuilder
    {
        private Dictionary<string, double> _fileToFractalValue;
        private Dictionary<string, LinesOfCode> _metrics;

        public IHierarchicalData Build(List<Artifact> summary,
                                      Dictionary<string, LinesOfCode> metrics,
                                      Dictionary<string, double> fileToFractalValue)
        {
            _metrics = metrics;
            _fileToFractalValue = fileToFractalValue;
            return Build(summary);
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
            return item.ServerPath + "\nFragmentation: " + GetWeight(item).ToString("F5");
        }

        protected override double GetWeight(Artifact item)
        {
            // Fractal value
            var key = item.LocalPath.ToLowerInvariant();
            return _fileToFractalValue[key];
        }

        protected override bool GetWeightIsAlreadyNormalized()
        {
            // Fractal value is already in range  [0...1)
            return true;
        }

        protected override bool IsAccepted(Artifact item)
        {
            // Area must > 0 because of division.
            var area = GetArea(item);

            return area > 1;
        }
    }
}