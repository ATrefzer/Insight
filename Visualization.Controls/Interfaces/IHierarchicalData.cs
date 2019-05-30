using System;
using System.Collections.Generic;
using Visualization.Controls.Data;

namespace Visualization.Controls.Interfaces
{
    public interface IHierarchicalData
    {
        /// <summary>
        /// Attached layouting information. This proeprty is not cloned
        /// </summary>
        LayoutInfo Layout { get; set; }

        string ColorKey { get; set; }
        double AreaMetricSum { get; }
        IReadOnlyCollection<IHierarchicalData> Children { get; }
        bool IsLeafNode { get; }
        double NormalizedWeightMetric { get; }
        string Name { get; }
        double AreaMetric { get; }
        double WeightMetric { get; }
        object Tag { get; set; }
        IHierarchicalData Parent { get; set; }
        string Description { get; set; }

        IHierarchicalData Clone();
        int CountLeafNodes();
        string GetPathToRoot();
        void NormalizeWeightMetrics();
        void RemoveLeafNodes(Func<IHierarchicalData, bool> removePredicate);
        void RemoveLeafNodesWithoutArea();
        IHierarchicalData Shrink();
        void SumAreaMetrics();
        void TraverseBottomUp(Action<IHierarchicalData> action);
        void TraverseTopDown(Action<IHierarchicalData> action);
    }
}