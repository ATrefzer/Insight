using Visualization.Controls.Data;
using Visualization.Controls.Interfaces;

namespace Visualization.Controls
{
    public sealed class HierarchicalDataContext
    {
        public HierarchicalDataContext(IHierarchicalData data, IColorScheme colorScheme)
        {
            Data = data;
            ColorScheme = colorScheme;
        }

        public HierarchicalDataContext Clone()
        {
            return new HierarchicalDataContext(Data.Clone(), ColorScheme);
        }

        public HierarchicalDataContext(HierarchicalData data)
        {
            Data = data;
            ColorScheme = new ColorScheme();
        }

        public IColorScheme ColorScheme { get; }

        public IHierarchicalData Data { get; }
    }
}