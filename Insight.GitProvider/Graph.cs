using System;
using System.Collections.Generic;
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
        /// Empty merge commits are removed implicitely
        /// In each commit remove the files that share the same history
        /// For each file to remove we traverse the whole graph from the starting commit.
        /// </summary>
        public void DeleteSharedHistory(List<ChangeSet> commits, Dictionary<string, string> filesToRemove)
        {
            lock (_lockObj)
            {
                // filesToRemove: 
                // fileId -> commit hash (change set id) where we start removing the file
                var lookup = commits.ToDictionary(x => x.Id, x => x);
                foreach (var fileToRemove in filesToRemove)
                {
                    var fileIdToRemove = fileToRemove.Key;
                    var changeSetId = fileToRemove.Value;

                    // Traverse graph to find all change sets where we have to delete the files
                    var nodesToProcess = new Queue<GraphNode>();
                    GraphNode node;
                    if (_graph.TryGetValue(changeSetId, out node))
                    {
                        nodesToProcess.Enqueue(node);
                    }

                    while (nodesToProcess.Any())
                    {
                        node = nodesToProcess.Dequeue();
                        var cs = lookup[node.CommitHash];

                        // Remove the file from change set
                        cs.Items.RemoveAll(i => i.Id == fileIdToRemove);

                        foreach (var parent in node.ParentHashes)
                        {
                            if (_graph.TryGetValue(parent, out node))
                            {
                                nodesToProcess.Enqueue(node);
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
    }
}