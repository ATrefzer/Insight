using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Insight.Shared.Model;

namespace Insight.GitProvider
{
    public sealed class GraphNode
    {
        public GraphNode(string commitHash)
        {
            CommitHash = commitHash;
        }

        public string CommitHash { get;  }

        /// <summary>
        /// Ordered list of all parents. The first parent is the branch that was checked out during a merge.
        /// The second parent is the branch that was merged in.
        /// </summary>
        public List<string> ParentHashes { get; } = new List<string>();

        public HashSet<string> ChildHashes { get; } = new HashSet<string>();
    }

    /// <summary>
    /// Full Git graph containing all commits. Nothing simplified here.
    /// </summary>
    public class Graph
    {
        private readonly object _lockObj = new object();

        // hash -> node {hash, parent hashes}
        readonly Dictionary<string, GraphNode> _graph = new Dictionary<string, GraphNode>();

        public IEnumerable<GraphNode> AllNodes => _graph.Values.ToList();
       

        /// <summary>
        /// Adds a new commit to the commit graph.
        /// This may result in several new nodes in the graph because we add nodes for possible parents in advance.
        /// </summary>
        /// <param name="hash">Commit hash (change set id)</param>
        /// <param name="parents">List of parent commit hashes</param>
        public void UpdateGraph(string hash, string parents)
        {
            var allParents = !string.IsNullOrEmpty(parents) ? parents.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(parent => parent)
                                    .ToList() : new List<string>();


            lock (_lockObj)
            {

                // GraphNode for the given hash.
                var node = GetOrAddNode(hash);

                // Update parents and child relationships
                foreach (var parentHash in allParents)
                {
                    node.ParentHashes.Add(parentHash);

                    var parent = GetOrAddNode(parentHash);
                    parent.ChildHashes.Add(hash);
                }
            }
        }

        GraphNode GetOrAddNode(string hash)
        {
            GraphNode node;
            if (_graph.TryGetValue(hash, out node) is false)
            {
                node = new GraphNode(hash);
                _graph.Add(hash, node);
            }

            return node;
        }

        public HashSet<string> GetChildren(string hash)
        {
            return _graph[hash].ChildHashes;
        }

        public List<string> GetParents(string hash)
        {
            return _graph[hash].ParentHashes;
        }

        public bool Exists(string hash)
        {
            return _graph.ContainsKey(hash);
        }

        public GraphNode GetNode(string hash)
        {
            if (hash == null)
            {
                return null;
            }

            return _graph[hash];
        }

        public bool TryGetNode(string hash, out GraphNode node)
        {
            if (hash == null)
            {
                node = null;
                return false;
            }

            return _graph.TryGetValue(hash, out node);
        }
    }
}