using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Insight.Shared.Model;

namespace Insight.GitProvider
{
    public class Graph
    {
        private sealed class GraphNode
        {
            public string CommitHash { get; set; }
            public List<string> ParentHashes { get; set; }
        }

        private object _lockObj = new object();

        // hash -> node {hash, parent hashes}
        Dictionary<string, GraphNode> _graph = new Dictionary<string, GraphNode>();

        /// <summary>
        /// Empty merge commits are removed implicitly
        /// In each commit remove the files that share the same history
        /// For each file to remove we traverse the whole graph from the starting commit.
        /// </summary>
        public void DeleteSharedHistory(List<ChangeSet> commits, Dictionary<string, HashSet<string>> filesToRemove)
        {
            lock (_lockObj)
            {
                // filesToRemove: 
                // fileId -> commit hash (change set id) where we start removing the file
                var lookup = commits.ToDictionary(x => x.Id, x => x);
                foreach (var fileToRemove in filesToRemove)
                {
                    var fileIdToRemove = fileToRemove.Key;
                    var changeSetIds = fileToRemove.Value;

                    // Traverse graph to find all change sets where we have to delete the files
                    var nodesToProcess = new Queue<GraphNode>();
                    var handledNodes = new HashSet<string>();
                    GraphNode node;

                    foreach (var csId in changeSetIds)
                    {
                        if (_graph.TryGetValue(csId, out node))
                        {
                            nodesToProcess.Enqueue(node);
                            handledNodes.Add(node.CommitHash);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                  

                    while (nodesToProcess.Any())
                    {
                        node = nodesToProcess.Dequeue();

                        if (lookup.ContainsKey(node.CommitHash))
                        {
                            // Note: The history of a file may skip some commits if they are not relevant.
                            // Therefore it is possible that no changeset exists for the hash.
                            // Remember that we follow the full graph.
                            var cs = lookup[node.CommitHash];

                            // Remove the file from change set
                            cs.Items.RemoveAll(i => i.Id == fileIdToRemove);
                        }
                        
                        foreach (var parent in node.ParentHashes)
                        {
                            // Avoid cycles in case a change set is parent many others.
                            if (!handledNodes.Contains(parent) && _graph.TryGetValue(parent, out node))
                            {
                                nodesToProcess.Enqueue(node);
                                handledNodes.Add(node.CommitHash);
                            }
                        }
                    }
                }
            }
        }

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
    }
}