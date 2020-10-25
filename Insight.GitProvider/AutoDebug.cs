using Insight.Shared.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Insight.GitProvider
{
    internal sealed class GitDebugHelper : IDisposable
    {
        private readonly string _directory;
        private readonly GitCommandLine _gitCli;
        private readonly StreamWriter _debugLogFile;

        public GitDebugHelper(string directory, GitCommandLine cmd)
        {
            _gitCli = cmd;
            
            _debugLogFile = File.CreateText(Path.Combine(directory, "debug.txt"));
            _debugLogFile.AutoFlush = true;
            _directory = directory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void Log(string message)
        {
            _debugLogFile.WriteLine(message);
        }

        public HashSet<string> GetAllTrackedFiles(string hash)
        {
            var serverPaths = _gitCli.GetAllTrackedFiles(hash);
            var all = serverPaths.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(all);
        }

        public bool Verify(Graph graph, GraphNode node, ChangeSetHistory history)
        {
            var expectedServerPaths = GetAllTrackedFiles(node.CommitHash);
            var actualServerPaths = node.Scope.GetAllFiles();

            var intersect = expectedServerPaths.Intersect(actualServerPaths).ToHashSet();
            var inGit = expectedServerPaths.Except(intersect).ToHashSet();
            var inScope = actualServerPaths.Except(intersect).ToHashSet();

            //var differences = expectedServerPaths;
            //differences.SymmetricExceptWith(actualServerPaths);

            var union = inScope.Union(inGit).ToList();
            if (union.Any())
            {
                // Save differences
                _debugLogFile.WriteLine("Deviation from scope and expected git tree");
                WriteDifference(inGit, "Git");
                WriteDifference(inScope, "Scope");

                // Save graphs
                foreach (var serverPath in union)
                {
                    var fi = new FileInfo(serverPath);
                    WriteDebugGraph(history, graph, node.CommitHash, fi.Name);
                }

                // Write differences to a separate file
                //File.WriteAllText(Path.Combine(_directory, $"conflict_diff_{shortHash}.txt"), builder.ToString());

                return false;
            }

            return true;
        }

        private void WriteDifference(HashSet<string> serverPaths, string header)
        {
            var builder = new StringBuilder();
            WriteDifference(builder, serverPaths, header);
            _debugLogFile.WriteLine(builder.ToString());
        }

        private static void WriteDifference(StringBuilder builder, HashSet<string> serverPaths, string header)
        {
            if (serverPaths.Any())
            {
                builder.AppendLine(header);
                foreach (var serverPath in serverPaths)
                {
                    builder.AppendLine(serverPath);
                }
            }
        }

        public void WriteDebugGraph(ChangeSetHistory history, Graph graph, string targetHash, string findMe)
        {
            var dbgGraph = graph.Clone();

            dbgGraph.MinimizeTo(targetHash);


            var highlightedNodes = new Dictionary<GraphNode, string>();
            foreach (var node in dbgGraph.AllNodes)
            {
                var changeSet = history.ChangeSets.Single(cs => cs.Id == node.CommitHash);
                var actionsDone = changeSet.Items.Where(item => item.ServerPath.Contains(findMe))
                    .Select(FormatItem).ToList();
                if (actionsDone.Any())
                {
                    highlightedNodes.Add(node, string.Join("\n", actionsDone));
                }
            }

            var shortHash = targetHash.Substring(0, 5);
            var file = Path.Combine(_directory, $"conflict_graph_{shortHash}_{findMe}.dot");

            var graphWriter = new GraphWriter();
            graphWriter.SaveGraphSimplified(file, dbgGraph, highlightedNodes);
        }

        private string FormatItem(ChangeItem item)
        {
            var builder = new StringBuilder();
            builder.Append(item.Kind);
            builder.Append(" ");
            if (item.FromServerPath != null)
            {
                builder.Append(item.FromServerPath);
                builder.Append(" -> ");
            }

            builder.Append(item.ServerPath);
            return builder.ToString();
        }

        public void Dispose()
        {
            _debugLogFile?.Dispose();
        }
    }
}