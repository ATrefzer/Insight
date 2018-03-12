using System;
using System.Collections.Generic;
using System.Linq;

using Insight.Shared.Extensions;
using Insight.Shared.Model;

using Visualization.Controls.Data;

namespace Insight.Builder
{
    public abstract class HierarchyBuilder
    {
        protected HierarchicalData Build(List<Artifact> artifacts)
        {
            var data = BuildHierarchy(artifacts);

            try
            {
                data.RemoveLeafNodesWithoutArea(); // throws if nothing is left
                data.SumAreaMetrics();
                data.NormalizeWeightMetrics();
            }
            catch (Exception)
            {
                return HierarchicalData.NoData();
            }

            return data.Shrink();
        }

        protected abstract double GetArea(Artifact item);

        protected HierarchicalData GetBranch(HierarchicalData parent, string branch)
        {
            var found = parent.Children.FirstOrDefault(child => child.Name == branch);
            if (found == null)
            {
                var newBranch = new HierarchicalData(branch, GetWeightIsAlreadyNormalized());
                parent.AddChild(newBranch);

                // Call when parent relation is set.
                newBranch.Description = GetDescription(newBranch);
                found = newBranch;
            }

            return found;
        }

        protected virtual string GetColorKey(Artifact item)
        {
            return null;
        }

        protected abstract string GetDescription(Artifact item);

        protected virtual double GetWeight(Artifact item)
        {
            return 0.0;
        }

        /// <summary>
        /// Returns if the weight metric is already normalized or not. (For example fractal value)
        /// Override only if the weight is normalized.
        /// </summary>
        protected virtual bool GetWeightIsAlreadyNormalized()
        {
            return false;
        }

        protected virtual bool IsAccepted(Artifact item)
        {
            return true;
        }


        /// <summary>
        ///     Each part in the file path is used as a branch that contains the remaining of the file path.
        ///     The file name itself corresponds to a leaf node that holds the weight and size.
        /// </summary>
        private HierarchicalData BuildHierarchy(List<Artifact> items)
        {
            // Later removed if not needed.
            // Note that the empty root node takes care that the / appears in front of every path.
            var artificialRoot = new HierarchicalData("", GetWeightIsAlreadyNormalized());

            foreach (var artifact in items)
            {
                // Note that the name is separated by slashes
                var parts = artifact.ServerPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                Insert(artificialRoot, artifact, parts);
            }

            if (artificialRoot.Children.Count == 1)
            {
                // Skip artificial root node if data provides its own.
                var root = artificialRoot.Children.First();
                root.Parent = null;
                return root;
            }

            return artificialRoot;
        }

        /// <summary>
        /// Gets description for a branch / folder
        /// </summary>
        private string GetDescription(HierarchicalData branch)
        {
            return branch.GetPathToRoot();
        }

        private void Insert(HierarchicalData parent, Artifact item, string[] parts)
        {
            if (parts.Length == 1)
            {
                InsertLeaf(parent, item, parts[0]);
            }
            else
            {
                var nextBranch = parts[0];

                // Find or create child structure element
                var branch = GetBranch(parent, nextBranch);
                Insert(branch, item, parts.Subset(1));
            }
        }

        private void InsertLeaf(HierarchicalData parent, Artifact item, string leafName)
        {
            if (!IsAccepted(item))
            {
                // Division 0. No area = no code lines, no weight = no commits
                return;
            }

            var leaf = new HierarchicalData(leafName, GetArea(item), GetWeight(item), GetWeightIsAlreadyNormalized());
            leaf.Description = GetDescription(item);
            leaf.ColorKey = GetColorKey(item);
            leaf.Tag = item.LocalPath;
            parent.AddChild(leaf);
        }
    }
}