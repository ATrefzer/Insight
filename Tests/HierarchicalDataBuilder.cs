using System;
using System.Diagnostics;
using System.IO;

using Visualization.Controls;
using Visualization.Controls.Data;

namespace Tests
{
    internal sealed class HierarchicalDataBuilder
    {
        private readonly Random _random = new Random(355);

        public HierarchicalDataContext CreateHierarchyFromFilesystem(string path, bool subDirs)
        {
            var item = new HierarchicalData(path);
            FillChildren(item, subDirs);

            item.RemoveLeafNodesWithoutArea();
            item.SumAreaMetrics();
            item.NormalizeWeightMetrics();

            Debug.WriteLine("Nodes: " + item.CountLeafNodes());
            return new HierarchicalDataContext(item);
        }

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

        internal HierarchicalData GetNumberOfCircles(int circles, int radius)
        {
            var root = new HierarchicalData("");
            for (var i = 0; i < circles; i++)
            {
                root.AddChild(new HierarchicalData("i", radius));
            }

            root.SumAreaMetrics();
            return root;
        }

        private void FillChildren(HierarchicalData root, bool recursive)
        {
            try
            {
                // Files (leaf nodes)
                var files = Directory.EnumerateFiles(root.Name);
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (fi.Length > 1000)
                    {
                        // Skip 0 files. Division by 0.
                        root.AddChild(new HierarchicalData(file, fi.Length, _random.Next(-1, 345)));
                    }
                }
            }
            catch (Exception)
            {
                // 
            }

            if (recursive == false)
            {
                return;
            }

            var subDirs = Directory.EnumerateDirectories(root.Name);
            try
            {
                foreach (var dir in subDirs)
                {
                    var subTreeRoot = new HierarchicalData(dir, 0);
                    root.AddChild(subTreeRoot);
                    FillChildren(subTreeRoot, true);
                }
            }
            catch (Exception)
            {
                //
            }
        }
    }
}