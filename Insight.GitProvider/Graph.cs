using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Insight.Shared.Model;

namespace Insight.GitProvider
{
    public sealed class GraphNode
    {
        public string CommitHash { get; set; }
        public List<string> ParentHashes { get; set; }
    }

    /// <summary>
    /// Full Git graph containing all commits. Nothing simplified here.
    /// </summary>
    public class Graph
    {
        private object _lockObj = new object();

        // hash -> node {hash, parent hashes}
        Dictionary<string, GraphNode> _graph = new Dictionary<string, GraphNode>();

       

        /// <summary>
        /// Adds a new commit to the commit Graph
        /// </summary>
        /// <param name="hash">Commit hash (change set id)</param>
        /// <param name="parents">List of parent commit hashes</param>
        public void UpdateGraph(string hash, string parents)
        {
            var allParents = !string.IsNullOrEmpty(parents) ? parents.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(parent => parent)
                                    .ToList() : new List<string>();
            var node = new GraphNode { CommitHash = hash, ParentHashes = allParents };

            lock (_lockObj)
            {
                if (!_graph.ContainsKey(node.CommitHash))
                {
                    _graph.Add(node.CommitHash, node);
                }
            }
        }

        public List<string> GetParents(string id)
        {
            return _graph[id].ParentHashes;
        }

        public bool Exists(string id)
        {
            return _graph.ContainsKey(id);
        }

        internal bool TryGetValue(string parent, out GraphNode node)
        {
            return _graph.TryGetValue(parent, out node);
        }
    }
}