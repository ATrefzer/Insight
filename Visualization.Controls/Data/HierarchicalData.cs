using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using Visualization.Controls.TreeMap;
using Visualization.Controls.Utility;

namespace Visualization.Controls.Data
{
    /// <summary>
    /// Coloring:
    /// For a leaf node:
    ///  - If a color key is set (not null) it is used for rendering
    ///  - If not the weight metric is used to derive a color
    ///  - If the weight is 0 the default color (light gray is used)
    /// For a non leaf node
    ///  - Typically the weight is 0 here, so we render hierarchy
    ///    with the default color. The weight is only set for leaf nodes.
    /// Metrics:
    /// If an area is not set excpicitely it is NaN. This is in many algorithms used for folders.
    /// If we remove leaf nodes and an inner node becomes a leaf now, its area is still NaN.
    /// Now we can call RemoveLeafNodesWithoutArea to remove these nodes (recursively).
    /// The AreaMeticSum however is initialized with 0.
    /// </summary>
    [Serializable]
    public sealed class HierarchicalData
    {
        private const string PathSeparator = "/";

        private readonly List<HierarchicalData> _children = new List<HierarchicalData>();
        private readonly bool _weightIsAleadyNormalized;

        public HierarchicalData(string name)
        {
            Name = name;
            Description = Name;
            AreaMetric = double.NaN;
            AreaMetricSum = 0.0;
            WeightMetric = 0.0;
            NormalizedWeightMetric = 0.0;
            _weightIsAleadyNormalized = true;
        }

        /// <summary>
        /// Leaf node must provide an area metric.
        /// </summary>
        public HierarchicalData(string name, double areaMetric)
        {
            Name = name;
            Description = Name;
            AreaMetric = areaMetric;
            AreaMetricSum = 0.0;
            WeightMetric = 0.0;
            NormalizedWeightMetric = 0.0;
            _weightIsAleadyNormalized = true;
        }

        public HierarchicalData(string name, double areaMetric, double weightMetric, bool weightIsAleadyNormalized = false)
        {
            Name = name;
            Description = Name;
            AreaMetric = areaMetric;
            AreaMetricSum = 0.0;
            WeightMetric = weightMetric;
            NormalizedWeightMetric = 0.0;
            _weightIsAleadyNormalized = weightIsAleadyNormalized;

            if (_weightIsAleadyNormalized)
            {
                if (WeightMetric < 0.0 || WeightMetric > 1)
                {
                    throw new ArgumentException("Normalized weight not in range [0,1]");
                }
            }
        }

        public double AreaMetric { get; }

        public double AreaMetricSum { get; private set; }

        public IReadOnlyCollection<HierarchicalData> Children => _children.AsReadOnly();

        public string ColorKey { get; set; }

        public string Description { get; set; }

        public bool IsLeafNode => Children.Count == 0;
        public LayoutInfo Layout { get; set; }

        public string Name { get; }

        public double NormalizedWeightMetric { get; private set; }

        public HierarchicalData Parent { get; set; }

        /// <summary>
        /// Needs to be serializable
        /// </summary>
        public object Tag { get; set; }

        public double WeightMetric { get; }


        public static HierarchicalData NoData()
        {
            return new HierarchicalData("NO DATA", 1);
        }

        public void AddChild(HierarchicalData child)
        {
            _children.Add(child);
            child.Parent = this;
        }

        /// <summary>
        /// No layouting information!
        /// </summary>
        public HierarchicalData Clone()
        {
            var root = Clone(this);
            root.NormalizeWeightMetrics();
            root.SumAreaMetrics();
            return root;
        }

        /// <summary>
        /// Returns the number of all tree nodes in the sub tree.
        /// </summary>
        public int CountLeafNodes()
        {
            if (IsLeafNode)
            {
                return 1;
            }

            var count = 0;
            foreach (var child in Children)
            {
                count += child.CountLeafNodes();
            }

            return count;
        }

        public void Dump()
        {
            Dump(this, 0);
        }


        public Range<double> GetMinMaxArea()
        {
            var min = double.MaxValue;
            var max = 0.0;

            GetMinMaxArea(ref min, ref max);

            return new Range<double>(min, max);
        }

        public Range<double> GetMinMaxWeight()
        {
            var min = double.MaxValue;
            var max = 0.0;

            GetMinMaxWeight(ref min, ref max);

            return new Range<double>(min, max);
        }

        public string GetPathToRoot()
        {
            var path = new List<string>();
            var current = this;
            while (current != null)
            {
                path.Add(current.Name);
                current = current.Parent;
            }

            path.Reverse();

            // Note that an artificial root node with name "" takes automatically care
            // that the path starts with a /. But we want to do so in all cases.
            var description = string.Join(PathSeparator, path);
            if (!description.StartsWith(PathSeparator, StringComparison.InvariantCulture))
            {
                description = PathSeparator + description;
            }

            return description;
        }

        public HierarchicalData Hit(HierarchicalData item, Point mousePos)
        {
            // We may find a more detailed hit deeper.
            HierarchicalData best = null;
            if (item.Layout == null)
            {
                return null;
            }

            if (item.Layout.IsHit(mousePos))
            {
                best = item;
                if (item.IsLeafNode)
                {
                    return item;
                }
            }

            foreach (var child in item.Children)
            {
                if (child.Layout.IsHit(mousePos))
                {
                    return Hit(child, mousePos);
                }
            }

            return best;
        }

        /// <summary>
        /// Layout must have been called
        /// </summary>
        public HierarchicalData Hit(Point mousePos)
        {
            return Hit(this, mousePos);
        }

        /// <summary>
        /// The weight metric is normalized only accross the leaf nodes.
        /// </summary>
        public void NormalizeWeightMetrics()
        {
            if (_weightIsAleadyNormalized)
            {
                return;
            }

            // Get min and max of weight metric and map to range 0...1.
            var range = GetMinMaxWeight();
            var min = range.Min;
            var max = range.Max;
            var scale = max - min;

            NormalizeWeightMetrics(min, scale);
        }

        /// <summary>
        /// Note that during the process new leaf nodes may arise.
        /// Call RemoveLeafNodesWithoutArea to remove them.
        /// </summary>
        public void RemoveLeafNodes(Func<HierarchicalData, bool> removePredicate)
        {
            RemoveLeafNodes(this, removePredicate);
        }

        /// <summary>
        /// Removes leaf node where area is not set.
        /// If new leaf nodes arise during the process they are also removed!
        /// </summary>
        public void RemoveLeafNodesWithoutArea()
        {
            RemoveLeafNodesWithoutArea(this);

            if (IsLeafNode && double.IsNaN(AreaMetric))
            {
                throw new Exception("Hierarchical data is not valid. Singular root node does not have an area.");
            }
        }

        public HierarchicalData Shrink()
        {
            if (Children.Count == 1)
            {
                return Children.First().Shrink();
            }

            // Leaf node or more than one children.
            return this;
        }

        /// <summary>
        /// Updates the area metrics from the children up to the root node.
        /// </summary>
        public void SumAreaMetrics()
        {
            if (IsLeafNode)
            {
                if (double.IsNaN(AreaMetric))
                {
                    throw new ArgumentException("Area metric is unknown for leaf node");
                }

                if (Math.Abs(AreaMetric) < double.Epsilon)
                {
                    throw new ArgumentException("Area metric is 0. This is not allowed.");
                }

                AreaMetricSum = AreaMetric;
                return;
            }

            // Non leaf node
            var sum = 0.0;
            foreach (var child in Children)
            {
                child.SumAreaMetrics();
                sum += child.AreaMetricSum;
            }

            AreaMetricSum = sum;

            // Treemap algorithm works best if processed in decreasing order
            _children.Sort(new DecreasingByAreaMetricSumComparer());
        }

        public void TraverseBottomUp(Action<HierarchicalData> action)
        {
            foreach (var child in Children)
            {
                child.TraverseBottomUp(action);
            }

            // First children, then the parent nodes
            action(this);
        }

        public void TraverseTopDown(Action<HierarchicalData> action)
        {
            action(this);

            foreach (var child in Children)
            {
                child.TraverseBottomUp(action);
            }
        }

        public void Verify()
        {
            TraverseBottomUp(x =>
                             {
                                 if (!double.IsNaN(x.AreaMetricSum))
                                 {
                                     Debugger.Break();
                                 }
                             });
        }

        private HierarchicalData Clone(HierarchicalData cloneThis)
        {
            var newData = new HierarchicalData(cloneThis.Name, cloneThis.AreaMetric, cloneThis.WeightMetric, _weightIsAleadyNormalized);
            newData.Description = cloneThis.Description;
            newData.ColorKey = cloneThis.ColorKey;
            newData.Tag = cloneThis.Tag;

            foreach (var child in cloneThis.Children)
            {
                newData.AddChild(Clone(child));
            }

            return newData;
        }

        private void Dump(HierarchicalData item, int level)
        {
            Debug.WriteLine(new string(Enumerable.Repeat('\t', level).ToArray()) + item.Name + " " + item.Layout);

            foreach (var child in item.Children)
            {
                Dump(child, level + 1);
            }
        }

        private void GetMinMaxArea(ref double min, ref double max)
        {
            if (Children.Count == 0)
            {
                min = Math.Min(min, AreaMetric);
                max = Math.Max(max, AreaMetric);
            }

            foreach (var child in Children)
            {
                child.GetMinMaxArea(ref min, ref max);
            }
        }

        private void GetMinMaxWeight(ref double min, ref double max)
        {
            if (Children.Count == 0)
            {
                min = Math.Min(min, WeightMetric);
                max = Math.Max(max, WeightMetric);
            }

            foreach (var child in Children)
            {
                child.GetMinMaxWeight(ref min, ref max);
            }
        }

        private void NormalizeWeightMetrics(double min, double scale)
        {
            if (IsLeafNode)
            {
                NormalizedWeightMetric = (WeightMetric - min) / scale;
            }

            foreach (var child in Children)
            {
                child.NormalizeWeightMetrics(min, scale);
            }
        }

        private void RemoveLeafNodes(HierarchicalData root, Func<HierarchicalData, bool> removePredicate)
        {
            foreach (var child in root.Children)
            {
                RemoveLeafNodes(child, removePredicate);
            }

            root._children.RemoveAll(x => x.IsLeafNode && removePredicate(x));
        }

        private void RemoveLeafNodesWithoutArea(HierarchicalData data)
        {
            foreach (var child in data.Children)
            {
                RemoveLeafNodesWithoutArea(child);
            }

            // During the recursive process new empty nodes may arise. So bottom to top.
            data._children.RemoveAll(x => x.IsLeafNode && (double.IsNaN(x.AreaMetric) || Math.Abs(x.AreaMetric) <= 0));
        }


        public sealed class DecreasingByAreaMetricSumComparer : IComparer<HierarchicalData>
        {
            /// <summary>
            /// Sorts collection of hierarchical data in decreasing order of the area metric
            /// </summary>
            public int Compare(HierarchicalData x, HierarchicalData y)
            {
                if (x == null)
                {
                    throw new ArgumentNullException(nameof(x));
                }

                if (y == null)
                {
                    throw new ArgumentNullException(nameof(y));
                }

                if (x.AreaMetricSum < y.AreaMetricSum)
                {
                    return 1;
                }

                if (x.AreaMetricSum > y.AreaMetricSum)
                {
                    return -1;
                }

                return 0;
            }
        }
    }
}