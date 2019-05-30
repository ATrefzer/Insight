using System.Collections.Generic;
using Insight.Metrics;
using Insight.Shared.Model;

using Visualization.Controls.Data;
using Visualization.Controls.Interfaces;

namespace Insight.Builder
{
    /// <summary>
    /// Transforms the given artifact summary, code metrics and main developer per file into a knowledge map.
    /// </summary>
    public sealed class KnowledgeBuilder : HierarchyBuilder
    {
        private Dictionary<string, MainDeveloper> _mainDeveloper;
        private Dictionary<string, LinesOfCode> _metrics;
        private string _onlyThisDeveloper;

        public KnowledgeBuilder()
        {
            _onlyThisDeveloper = null;
        }

        public KnowledgeBuilder(string developer)
        {
            _onlyThisDeveloper = developer;
        }

        public IHierarchicalData Build(List<Artifact> summary,
                                      Dictionary<string, LinesOfCode> metrics,
                                      Dictionary<string, MainDeveloper> mainDeveloper)
        {
            _metrics = metrics;
            _mainDeveloper = mainDeveloper;
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

        protected override string GetColorKey(Artifact item)
        {
            var mainDev = GetMainDeveloper(item).Developer;

            if (_onlyThisDeveloper != null && mainDev != _onlyThisDeveloper)
            {
                // Null for all artifacts not provided by the developer of interest.
                return null;
            }

            return mainDev;
        }

        protected override string GetDescription(Artifact item)
        {
            var mainDev = GetMainDeveloper(item);
            return item.ServerPath + "\nCommits: " + item.Commits
                   + "\nLOC: " + GetArea(item)
                   + "\nMain developer: " + mainDev.Developer + " " + mainDev.Percent.ToString("F2") + "%";
        }

        protected override bool IsAccepted(Artifact item)
        {
            // Area must > 0 because of division.
            var area = GetArea(item);

            return area >= 1;
        }

        private MainDeveloper GetMainDeveloper(Artifact item)
        {
            var key = item.LocalPath.ToLowerInvariant();
            if (_mainDeveloper.ContainsKey(key))
            {
                return _mainDeveloper[key];
            }

            // Default color
            return new MainDeveloper("unknown", 0.0);
        }
    }
}