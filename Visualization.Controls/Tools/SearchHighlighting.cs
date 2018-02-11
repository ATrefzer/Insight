using Visualization.Controls.Data;

namespace Visualization.Controls.Tools
{
    internal sealed class SearchHighlighting : IHighlighting
    {
        private readonly string _pattern;

        public SearchHighlighting(string pattern)
        {
            _pattern = pattern?.ToLowerInvariant();
        }

        public bool IsHighlighted(HierarchicalData data)
        {
            if (string.IsNullOrEmpty(_pattern))
            {
                return false;
            }

            return data.Name.ToLowerInvariant().Contains(_pattern);
        }
    }
}