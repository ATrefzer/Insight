using System;

using Visualization.Controls;
using Visualization.Controls.Data;

namespace Tests
{
    internal sealed class HierarchicalTestExamples
    {
        public HierarchicalDataContext GetColoredNestedExample()
        {
            var root = new HierarchicalData("root");
            var scheme = new ColorScheme(new[] { "c1", "c2", "c3" });

            HierarchicalData child;
            child = new HierarchicalData("ra", 10);
            child.ColorKey = "c1";
            root.AddChild(child);
            child = new HierarchicalData("ra", 10);
            child.ColorKey = "c2";
            root.AddChild(child);
            child = new HierarchicalData("ra", 10);
            child.ColorKey = "c3";
            root.AddChild(child);
            child = new HierarchicalData("ra", 10);
            child.ColorKey = "unknown";
            root.AddChild(child);

            root.SumAreaMetrics();
            Console.WriteLine(root.CountLeafNodes());
            return new HierarchicalDataContext(root, scheme);
        }

        public HierarchicalDataContext GetFlatExample()
        {
            var root = new HierarchicalData("");
            root.AddChild(new HierarchicalData("6", 300, 8));
            root.AddChild(new HierarchicalData("6", 60, 7));
            root.AddChild(new HierarchicalData("4", 40, 6));
            root.AddChild(new HierarchicalData("3", 30, 6));
            root.AddChild(new HierarchicalData("1", 10, 6));
            root.AddChild(new HierarchicalData("2", 20, 200));
            root.AddChild(new HierarchicalData("2", 20, 1));

            var child = new HierarchicalData("3", 30, 1);
            root.AddChild(child);
            root.SumAreaMetrics();
            root.NormalizeWeightMetrics();
            return new HierarchicalDataContext(root);
        }

        public HierarchicalDataContext ShowCollisionWithLastElementProblem()
        {
            var root = new HierarchicalData("");
            root.AddChild(new HierarchicalData("6", 10));
            root.AddChild(new HierarchicalData("6", 10));
            root.AddChild(new HierarchicalData("4", 10));
            root.AddChild(new HierarchicalData("3", 10));
            root.AddChild(new HierarchicalData("1", 10));
            root.AddChild(new HierarchicalData("3", 10));
            root.AddChild(new HierarchicalData("1", 10));
            root.AddChild(new HierarchicalData("3", 10));
            root.AddChild(new HierarchicalData("1", 10));
            root.SumAreaMetrics();
            return new HierarchicalDataContext(root);
        }
    }
}