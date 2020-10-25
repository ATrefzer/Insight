using Insight.Shared;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Insight.GitProvider.Debugging;

namespace Insight.GitProvider
{
    public sealed class GitProvider : GitProviderBase, ISourceControlProvider
    {
        private GitDebugHelper _dbg;
        private Statistics _stats;

        public void Initialize(string projectBase, string cachePath, IFilter fileFilter, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;
            _fileFilter = fileFilter;

            _historyFile = Path.Combine(cachePath, "git_history.json");
            _contributionFile = Path.Combine(cachePath, "contribution.json");
            _gitCli = new GitCommandLine(_startDirectory);

            // "/" maps to _startDirectory
            _mapper = new PathMapper(_startDirectory);
        }

        public void UpdateCache(IProgress progress, bool includeWorkData)
        {
            DeleteAllCaches();

            VerifyGitPreConditions();
            var logDir = PrepareLogDirectory();

            using (_dbg = new GitDebugHelper(logDir, _gitCli))
            {
                UpdateHistory(progress);
            }

            if (includeWorkData)
            {
                // Optional
                UpdateContribution(progress);
            }
        }

        private void Log(string logMessage)
        {
            _dbg?.Log(logMessage);
        }

        public static string GetClass()
        {
            var type = typeof(GitProvider);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        private void DeleteAllCaches()
        {
            if (File.Exists(_contributionFile))
            {
                File.Delete(_contributionFile);
            }

            if (File.Exists(_historyFile))
            {
                File.Delete(_historyFile);
            }
        }

        private string PrepareLogDirectory()
        {
            var logPath = Path.Combine(_cachePath, "logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            return logPath;
        }

        private (ChangeSetHistory, Graph) ReadHistory()
        {
            // Ordered from new to old
            var fullLog = _gitCli.Log();
            var parser = new Parser(_mapper);
            var (history, graph) = parser.ParseLogString(fullLog);
            SaveFullGitLog(fullLog);

            return (history, graph);
        }

        private void UpdateHistory(IProgress progress)
        {
            Warnings = new List<WarningMessage>();
            _stats = new Statistics();

            var (history, graph) = ReadHistory();

            // I assume the only commit without children is the master's head
            Debug.Assert(HasUnmergedCommits(graph) is false);

            AssignUniqueFileIds(graph, history, progress);

            // Remove deleted files and empty changes sets
            var headNode = graph.GetNode(GetMasterHead());
            var aliveIds = GetAllTrackedFiles().Select(file => headNode.Scope.GetId(file)).ToHashSet();

            VerifyScope(headNode);

            // Note we have to drop all Delete items from the history. This is safe.
            // A file can be deleted in one branch but maintained in another one.
            // If we would call CleanupHistory() we would remove all ids that belong to a deleted file
            // This means we lose a file that is still tracked.

            history.CleanupHistory(aliveIds);
            Debug.Assert(!history.ChangeSets.SelectMany(cs => cs.Items).Any(item => item.IsDelete()));

            SaveHistory(history);
        }

        private void VerifyScope(GraphNode node)
        {
            if (node.Scope == null)
            {
                throw new Exception("Node has node scope assigned!");
            }

            var expectedServerPaths = GetAllTrackedFiles(node.CommitHash);
            var actualServerPaths = node.Scope.GetAllFiles();

            var differences = expectedServerPaths;
            differences.SymmetricExceptWith(actualServerPaths);
            foreach (var diff in differences)
            {
                Warnings.Add(new WarningMessage(node.CommitHash, diff));
            }
        }

        private bool IsMergeWithUnprocessedParents(GraphNode node)
        {
            if (IsMerge(node))
            {
                var mergeInto = node.Parents[0];
                var mergeFrom = node.Parents[1];

                if (mergeFrom.Scope == null || mergeInto.Scope == null)
                {
                    // We can't process this merge commit yet. But we end up here again.
                    return true;
                }
            }

            return false;
        }

        private void AssignUniqueFileIds(Graph graph, ChangeSetHistory history, IProgress progress)
        {
            var commitHashToChangeSet = history.ChangeSets.ToDictionary(cs => cs.Id, cs => cs);

            var alreadyProcessed = new HashSet<string>();

            // Can't use enumerator here because we have to visit unfinished parents again.
            var rootNode = graph.AllNodes.Single(node => node.Parents.Any() is false);
            var nodesToProcess = new Queue<GraphNode>();
            nodesToProcess.Enqueue(rootNode);

            var processing = 0;
            var nodeCount = graph.AllNodes.Count();
            while (nodesToProcess.Any())
            {
                var node = nodesToProcess.Dequeue();
                var changeSet = commitHashToChangeSet[node.CommitHash];

                Log("\n##############################################################################");
                Log($"Start processing node {node.CommitHash}");

                if (IsMergeWithUnprocessedParents(node))
                {
                    // We arrive here later again.
                    Log("  But this is an merge node with an unprocessed parent. See you later again.");
                    continue;
                }

                if (!alreadyProcessed.Add(node.CommitHash))
                {
                    // When we arrive a merge node the first time both parents may be complete (or not).
                    // If so we would end up following the same path twice.
                    Log("  But this node was already processed. Stop here.");
                    continue;
                }

                progress.Message($"Processing commit {++processing} / {nodeCount}");


                // Obtain scope for this node.
                var state = PreProcessNode(node);

                ApplyIdsAndUpdateScope(changeSet, state);

                var remainingAmbiguities = state.GetMergeAmbiguities().ToList();
                if (remainingAmbiguities.Any())
                {
                    
                    Log("\tWe have remaining ambiguities. Synch with git tree here.");
                    _stats.LookupTreeAfterBothBranchesRenamedFile++;
                    var actualTreeFiles = GetAllTrackedFiles(node.CommitHash);

                    // Remove all files no longer existent.
                    foreach (var pair in remainingAmbiguities)
                    {
                        if (actualTreeFiles.Contains(pair.Key))
                        {
                            // I know the Id is unique here.
                            Log($"\t - Include tracked: {pair.Key} ({pair.Value})");
                            Debug.Assert(!state.NewScope.IsKnown(pair.Key));
                            state.Resolve(pair.Key, Resolution.Keep);
                        }
                        else
                        {
                            Log($"\t - Drop not tracked: {pair.Key} ({pair.Value})");
                            state.Resolve(pair.Key, Resolution.Remove);
                        }
                    }
                }

                node.Scope = state.NewScope;
                //_dbg?.Verify(graph, node, history);

                AddChildrenToQueue(node, nodesToProcess);
            }
        }

        /// <summary>
        /// Obtains a new scope for the current node together with ambiguities from parent nodes.
        /// </summary>
        private ResolutionState PreProcessNode(GraphNode node)
        {
            Log("\tPreprocess node");
            var state = new ResolutionState();
            
            if (IsRoot(node))
            {
                // Called once for the first commit.
                Log("  This is the first node. So I create a new scope.");
                state.NewScope = new Scope();
            }
            else if (node.Parents.Count == 1)
            {
                var parent = node.Parents.Single();
                state.NewScope = parent.Scope;

                Debug.Assert(state.NewScope != null);
                if (parent.Children.Count > 1)
                {
                    Log("  One parent, but parent has many children so I inherit its scope by cloning");

                    // We follow two branches, so each one gets its own copy.
                    state.NewScope = parent.Scope.Clone();
                }
                else
                {
                    state.NewScope = parent.Scope;
                    Log("  One parent, one child, so I inherit its original scope");
                }
            }
            else if (IsMerge(node))
            {
                Log("... this is a merge node.");

                var mergeInto = node.Parents[0];
                var mergeFrom = node.Parents[1];

                Log($"Parents: {node.Parents[0].CommitHash} {node.Parents[1].CommitHash}");
                Log("I try to resolve the merge before processing the item list.");

                var noConflicts = mergeInto.Scope.Intersect(mergeFrom.Scope).ToList();

                state.OnlyInMergeInto = new Scope(mergeInto.Scope.Except(noConflicts));
                state.OnlyInMergeFrom = new Scope(mergeFrom.Scope.Except(noConflicts));
                state.NewScope = new Scope(noConflicts.ToDictionary(nc => nc.Key, nc => nc.Value));

                // A file was created and maintained in different branches. We reset the tracking here.
                ResetIdsWhenSamePathHasDifferentIds(state);
            }

            return state;
        }

        private static bool IsRoot(GraphNode node)
        {
            return node.Parents.Count == 0;
        }

        private void ResetIdsWhenSamePathHasDifferentIds(ResolutionState state)
        {
            // Here we lose some information. A file was added with the same name in both branches.
            // I reset tracking with a new Id from here on. We lose all commits before. 

            Log("\tPreprocess merge - handling same file tracked with different ids.");

            var samePathHasDifferentIds = state.GetMergeAmbiguities().GroupBy(pair => pair.Key).Where(g => g.Count() > 1).ToList();
            if (samePathHasDifferentIds.Any())
            {
                foreach (var group in samePathHasDifferentIds)
                {
                    var serverPath = group.Key;
                    var id = state.Resolve(serverPath, Resolution.KeepWithNewId);

                    _stats.RestartWithNewFileIdBecauseAddedInDifferentBranches++;
                    var msg = $"Resolve same file with different Ids: {serverPath}. Restart with new file id ({id})";
                    Warnings.Add(new WarningMessage("", msg));
                    Log($"\t - WARNING: { msg}");
                }
            }
        }

        private static void AddChildrenToQueue(GraphNode node, Queue<GraphNode> nodesToProcess)
        {
            // Add children to processing queue
            foreach (var childNode in node.Children)
            {
                nodesToProcess.Enqueue(childNode);
            }
        }

        private void ApplyIdsAndUpdateScope(ChangeSet changeSet, ResolutionState state)
        {
            Log("\tProcessing items");
            foreach (var item in changeSet.Items)
            {
                if (item.Id != null)
                {
                    // Partially merged
                    continue;
                }

                switch (item.Kind)
                {
                    case KindOfChange.Add:

                        // This file is in the new scope with a new id regardless what the parents have.
                        
                        item.Id = state.Resolve(item.ServerPath, Resolution.Add);
                        Log($"\tA {item.ServerPath} ({item.Id})");
                        break;

                    case KindOfChange.Edit:

                        item.Id = state.Resolve(item.ServerPath, Resolution.GetExisting);
                        Log($"\tM {item.ServerPath} ({item.Id})");
                        break;

                    case KindOfChange.Copy:

                        // Treat like Add
                        item.Id = state.Resolve(item.ServerPath, Resolution.Add);
                        Log($"\tC {item.ServerPath} ({item.Id})");
                        break;

                    case KindOfChange.Rename:

                        // Must exist in either scope
                        state.Resolve(item.FromServerPath, Resolution.Remove);
                        item.Id = state.Resolve(item.ServerPath, Resolution.Add);
                        Log($"\tR {item.FromServerPath} -> {item.ServerPath} ({item.Id})");
                        break;

                    case KindOfChange.Delete:
                       
                        item.Id = state.Resolve(item.ServerPath, Resolution.Remove);
                        Log($"\tD {item.ServerPath} ({item.Id})");

                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

        private bool HasUnmergedCommits(Graph graph)
        {
            // I assume the way I request the git log the only commit without children is the head (of the master)
            var headHash = GetMasterHead();
            var unfinished = graph.AllNodes.Where(node => node.Children.Any() is false && node.CommitHash != headHash);
            return unfinished.Any();
        }

        private void SaveFullGitLog(string fullLog)
        {
            File.WriteAllText(Path.Combine(_cachePath, @"git_full.txt"), fullLog);
        }

        private void SaveHistory(ChangeSetHistory history)
        {
            var json = JsonConvert.SerializeObject(history, Formatting.Indented);
            File.WriteAllText(_historyFile, json, Encoding.UTF8);
        }

        private bool IsMerge(GraphNode graphNode)
        {
            return graphNode.Parents.Count == 2;
        }
    }
}