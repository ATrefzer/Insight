using Visualization.Controls.Interfaces;

namespace Visualization.Controls
{
    /// <summary>
    /// Data context that is bound to the TreeMapView or CirclePackingView
    /// </summary>
    public sealed class HierarchicalDataContext
    {
        public HierarchicalDataContext(IHierarchicalData data, IColorScheme colorScheme)
        {
            Data = data;
            ColorScheme = colorScheme;
        }

        public HierarchicalDataContext Clone()
        {
            // Layout info is lost!
            var clone = new HierarchicalDataContext(Data.Clone(), ColorScheme);
            clone.WeightSemantic = WeightSemantic;
            clone.AreaSemantic = AreaSemantic;
            return clone;
        }

        public HierarchicalDataContext(IHierarchicalData data)
        {
            Data = data;
            ColorScheme = new ColorScheme();
        }

        public IColorScheme ColorScheme { get; }

        public IHierarchicalData Data { get; }

        /// <summary>
        /// User hint what the area means (file size)
        /// </summary>
        public string AreaSemantic { get; set; }

        /// <summary>
        /// User hint what the weight means (modifications)
        /// </summary>
        public string WeightSemantic { get; set; }
    }
}