using System;
using System.Collections.Generic;

using Visualization.Controls.Common;

namespace Visualization.Controls.Interfaces
{
    public interface IHierarchicalData : IEnumerable<IHierarchicalData>
    {
        // TODO move layoutig code outside.
        /// <summary>
        /// Attached layout information. This property is not cloned
        /// </summary>
        LayoutInfo Layout { get; set; }
        
        /// <summary>
        /// Unique Id of this node within the hierarchy. (Cloned)
        /// </summary>
        int Id { get; set; }


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