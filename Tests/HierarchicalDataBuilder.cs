using System;
using System.IO;
using Visualization.Controls.Data;

namespace Tests
{
    /// <summary>
    /// Random hierarchical test data-
    /// </summary>
    public class HierarchicalDataBuilder
    {
        private Random _random = new Random(DateTime.Now.Millisecond);


        public HierarchicalData CreateHierarchyFromFilesystem(string path, bool subDirs)
        {
            var item = new HierarchicalData(path);
            FillChildren(item, subDirs);

            item.RemoveLeafNodesWithoutArea();
            item.SumAreaMetrics();
            item.NormalizeWeightMetrics();

            return item;
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
                // Ignore file access rights.
            }

            if (!recursive)
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
                // Ignore file access rights.
            }
        }

        // Assume root has always at least one child!
        public HierarchicalData GenerateRandomHierarchy()
        {
            var root = new HierarchicalData("root");

            var depth = GetRandomDepth();

            // At least one child
            FillChildren(root, GetRandmomNumberOfChildren(), depth);

            return root;
        }


        private void FillChildren(HierarchicalData data, int numChildren, int depth)
        {
            depth--;
            for (int i = 0; i < numChildren; i++)
            {
                HierarchicalData newChild;
                if (GetRandomIsLeaf() || depth <= 0)
                {
                    newChild = new HierarchicalData("leaf", GetRandomArea());
                }
                else
                {
                    newChild = new HierarchicalData("folder");
                    FillChildren(newChild, GetRandmomNumberOfChildren(), depth);
                }
                data.AddChild(newChild);
            }
        }

        /// <summary>
        /// At least 1. When we call this it is already decided that we are a parent node.
        /// </summary>
        int GetRandmomNumberOfChildren()
        {
            return _random.Next(1, 10);
        }

        double GetRandomArea()
        {
            var value = _random.NextDouble() * _random.Next(1, 100000);
            return Math.Ceiling(value);
        }

        private int GetRandomDepth()
        {
            return _random.Next(0, 10);
        }

        bool GetRandomIsLeaf()
        {
            // Probability of being a leaf node = 0.2
            var value = _random.NextDouble();
            return value < 0.2;
        }
    }
}
