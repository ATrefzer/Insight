using Visualization.Controls.Data;

namespace Visualization.Controls.Tools
{
    internal sealed class Highlighting : IHighlighting
    {
        private readonly ToolViewModel _toolViewModel;
        private readonly string _pattern;

        public Highlighting(ToolViewModel tvm)
        {
            _toolViewModel = tvm;
            _pattern = tvm.SearchPattern?.ToLowerInvariant();
        }

        public bool IsHighlighted(HierarchicalData data)
        {
            bool patternMatch = false;
            if (!string.IsNullOrEmpty(_pattern))
            {
                patternMatch = data.Name.ToLowerInvariant().Contains(_pattern);
            }

           

            // Matching area and weight criteria is optional.
            bool filterMatch = false;
            if (_toolViewModel.NoFilterJustHighlight
                && _toolViewModel.IsAreaValid(data.AreaMetric) 
                && _toolViewModel.IsWeightValid(data.WeightMetric))
            {
                filterMatch = true;
            }

            return patternMatch || filterMatch;
        }
    }
}