using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Insight.GitProvider.Debugging
{
    public sealed class GraphWriter
    {
        private StringBuilder _builder;
        private Dictionary<GraphNode, string> _highlightedNodes;
        private HashSet<GraphNode> _processed;
        private const int Digits = 5;

        /// <summary>
        /// Writes the graph in .dot format to the disk.
        /// Straight sequences are simplified if not highlighted.
        /// All merge and branching nodes are kept regardless if highlighted or not.
        ///
        /// Note:
        /// Since a last visible parent may have multiple paths to the next drawn child
        /// multiple arrows may appear from one node to another. This means
        /// there were different paths. If it is a simplified path it is drawn blue,
        /// black otherwise.
        /// </summary>
        public void SaveGraphSimplified(string pathToFile, Graph graph, Dictionary<GraphNode, string> highlightedNodes)
        {
            var root = Initialize(graph, highlightedNodes);

            _builder.AppendLine("digraph G {");

            WriteColoredNodes();
            WriteEdgesRecursive(root, root);

            _builder.AppendLine("}");
            File.WriteAllText(pathToFile, _builder.ToString());

            Cleanup();
        }

        /// <summary>
        /// Writes the full graph in .dot format to the disk. Highlight nodes if specified.
        /// </summary>
        public void SaveGraph(string pathToFile, Graph graph, Dictionary<GraphNode, string> highlightedNodes = null)
        {
            var root = Initialize(graph, highlightedNodes);

            _builder.AppendLine("digraph G {");

            WriteColoredNodes();
            WriteEdgesRecursive(root);

            _builder.AppendLine("}");
            File.WriteAllText(pathToFile, _builder.ToString());

            Cleanup();
        }

        private void Cleanup()
        {
            _builder.Clear();
            _highlightedNodes?.Clear();
            _processed?.Clear();
        }

        private void WriteColoredNodes()
        {
            foreach (var node in _highlightedNodes)
            {
                _builder.AppendLine($"\"{node.Key.CommitHash.Substring(0, Digits)}\"[style=filled,fillcolor=red,xlabel=\"{node.Value}\"]");
            }
        }

        private GraphNode Initialize(Graph graph, Dictionary<GraphNode, string> highlightedNodes)
        {
            _builder = new StringBuilder();

            if (highlightedNodes != null)
            {
                _highlightedNodes = highlightedNodes;
            }
            else
            {
                _highlightedNodes = new Dictionary<GraphNode, string>();
            }

            var root = FindRoot(graph);

            _processed = new HashSet<GraphNode>();
            return root;
        }

        private static GraphNode FindRoot(Graph graph)
        {
            return graph.AllNodes.Single(node => node.Parents.Any() is false);
        }

        /// <summary>
        /// Recursively writes the edges. Nodes on a straight line cans be simplified.
        /// Tracking a single(!) last parent works only if we remove nodes on a straight line.
        /// </summary>
        private void WriteEdgesRecursive(GraphNode lastParent, GraphNode node)
        {
            if (!_processed.Add(node))
            {
                // We already processed this node by traversing another path.
                return;
            }


            foreach (var child in node.Children)
            {
                var skippedNodes = lastParent != node;

                if (IsHidden(child))
                {
                    // Move ahead without drawing. Works only if the skipped node has a single(!) parent.
                    // Otherwise there is no unique lastParent.
                    WriteEdgesRecursive(lastParent, child);
                }
                else
                {
                    WriteEdge(lastParent, child, skippedNodes);
                    WriteEdgesRecursive(child, child); // Use current child as next last parent
                }
            }
        }


        private void WriteEdgesRecursive(GraphNode node)
        {
            if (!_processed.Add(node))
            {
                // We already processed this node by traversing another path.
                return;
            }

            foreach (var child in node.Children)
            {
                WriteEdge(node, child, false);
            }

            foreach (var child in node.Children)
            {
                WriteEdgesRecursive(child);
            }
        }

        private string GetShortHash(GraphNode node)
        {
            return node.CommitHash.Substring(0, Digits);
        }

        private void WriteEdge(GraphNode parent, GraphNode node, bool colored)
        {
            if (!colored)
            {
                _builder.AppendLine($"\"{GetShortHash(parent)}\" -> \"{GetShortHash(node)}\"");
            }
            else
            {
                _builder.AppendLine($"\"{GetShortHash(parent)}\" -> \"{GetShortHash(node)}\" [color=blue]");
            }
        }

        private bool IsHighlighted(GraphNode node)
        {
            return _highlightedNodes.ContainsKey(node);
        }

        private bool IsPartOfStraightPath(GraphNode node)
        {
            return node.Parents.Count == 1 &&
                   node.Children.Count == 1;
        }

        private bool IsHidden(GraphNode node)
        {
            return IsPartOfStraightPath(node) && !IsHighlighted(node);
        }
    }
}