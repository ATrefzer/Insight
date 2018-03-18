using Visualization.Controls.Data;

namespace Visualization.Controls
{
    public sealed class HierarchicalDataContext
    {
        public HierarchicalDataContext(HierarchicalData data, ColorScheme colorScheme)
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

        public ColorScheme ColorScheme { get; }

        public HierarchicalData Data { get; }
    }
}