using System.Collections.Generic;
using Visualization.Controls.Data;

namespace Visualization.Controls.Interfaces
{
    public interface IHierarchicalData
    {
        TreeMap.LayoutInfo Layout { get; set; }
        string ColorKey { get; set; }
        double AreaMetricSum { get; }
        IReadOnlyCollection<Data.HierarchicalData> Children { get; }
        bool IsLeafNode { get; }
        double NormalizedWeightMetric { get; }
        string Name { get; }
        double AreaMetric { get; }
        double WeightMetric { get; }

        HierarchicalData Clone();
        HierarchicalData Hit(System.Windows.Point mousePos);
        void NormalizeWeightMetrics();
        void RemoveLeafNodes(System.Func<Data.HierarchicalData, bool> removePredicate);
        void RemoveLeafNodesWithoutArea();
        void SumAreaMetrics();
        void TraverseTopDown(System.Action<Data.HierarchicalData> action);
    }
}