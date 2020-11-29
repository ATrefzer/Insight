using Insight.GitProvider.Debugging;
using Insight.Shared;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Insight.GitProvider
{
    public sealed class GitProvider : GitProviderBase, ISourceControlProvider
    {
        private Repository _repo;
        private Statistics _stats;

        public void Initialize(string projectBase, string cachePath, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;

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

            using (_repo = new Repository(_startDirectory))
            {
                UpdateHistory(progress);
            }

            if (includeWorkData)
            {
                // Optional
                UpdateContribution(progress);
            }
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

        /// <summary>
        ///     Raw history. Nothing deleted or cleaned
        /// </summary>
        public (ChangeSetHistory, Graph) GetRawHistory(IProgress progress)
        {
            Graph graph;
            ChangeSetHistory history;
            using (var repo = new Repository(_startDirectory))
            {
                // All nodes in current branch reachable from the head.

                progress?.Message("Reading commit graph");
                graph = GetGraph(repo);

                progress?.Message("Creating the history");
                history = CreateHistory(repo, graph, progress);
            }

            return (history, graph);
        }


        private void UpdateHistory(IProgress progress)
        {
            Warnings = new List<WarningMessage>();
            _stats = new Statistics();

            var (history, graph) = GetRawHistory(progress);

            // Remove deleted files and empty changes sets
            var headNode = graph.GetNode(GetMasterHead());
            var allTrackedFiles = GetAllTrackedFiles(); // TODO use the tre (!)
            var aliveIds = allTrackedFiles.Select(file => headNode.Scope.GetId(file)).ToHashSet();

            VerifyScope(headNode);

            // Note we have to drop all Delete items from the history. This is safe.
            // A file can be deleted in one branch but maintained in another one.
            // If we would call CleanupHistory() we would remove all ids that belong to a deleted file
            // This means we lose a file that is still tracked.

            history.CleanupHistory(aliveIds);
            Debug.Assert(!history.ChangeSets.SelectMany(cs => cs.Items).Any(item => item.IsDelete()));

            SaveHistory(history);
        }

        private bool IsMerge(GraphNode graphNode)
        {
            return graphNode.Parents.Count == 2;
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

        /// <summary>
        ///     Creates the history in a form that we can process further from initial commit to the last one.
        /// </summary>
        private ChangeSetHistory CreateHistory(Repository repo, Graph graph, IProgress progress)
        {
            var nodeCount = graph.AllNodes.Count();
            var currentNode = 0;

            var changeSets = new List<ChangeSet>();

            var initialNodes = graph.AllNodes.Where(node => node.Parents.Any() is false).ToList();

            Debug.Assert(initialNodes.Count() == 1);
            var initialNode = initialNodes.First();

            var alreadyProcessed = new HashSet<string>();
            var nodesToProcess = new Queue<GraphNode>();
            nodesToProcess.Enqueue(initialNode);
            while (nodesToProcess.Any())
            {
                var node = nodesToProcess.Dequeue();

                if (IsMergeWithUnprocessedParents(node))
                {
                    // We arrive here later again.
                    //Log("  But this is an merge node with an unprocessed parent. See you later again.");
                    continue;
                }

                if (!alreadyProcessed.Add(node.CommitHash))
                {
                    // When we arrive a merge node the first time both parents may be complete (or not).
                    // If so we would end up following the same path twice.
                    //Log("  But this node was already processed. Stop here.");
                    continue;
                }

                progress?.Message($"Processing commit {++currentNode} / {nodeCount}");

                var commit = repo.Lookup<Commit>(node.CommitHash);
                var differences = CalculateDiffs(repo, commit);

                var (scope, deletedServerPathToId) = ApplyChangesToScope(node, differences);
                node.Scope = scope;


                var cs = CreateChangeSet(commit);
                changeSets.Add(cs);

                // Create change items
                foreach (var change in differences.ChangesInCommit)
                {
                    var item = CreateChangeItem(change, scope, deletedServerPathToId);
                    cs.Items.Add(item);
                }

                // Add children to processing queue
                foreach (var childNode in node.Children)
                {
                    nodesToProcess.Enqueue(childNode);
                }
            }

            VerifyScope(graph.GetNode(repo.Head.Tip.Sha));

            var history = new ChangeSetHistory(changeSets.OrderByDescending(cs => cs.Date).ToList());
            return history;
        }


        /// <summary>
        ///     Returns the deleted files, no longer in scope (server path -> id)
        /// </summary>
        private (Scope, Dictionary<string, string>) ApplyChangesToScope(GraphNode node, Differences deltas)
        {
            var deletedServerPathToId = new Dictionary<string, string>();
            Scope scope = null;

            if (node.Parents.Count == 0)
            {
                // Called once for the first commit.
                //Log("  This is the first node. So I create a new scope.");
                scope = new Scope();
                UpdateScopeSingleOrNoParent(deltas, scope, deletedServerPathToId);
            }
            else if (node.Parents.Count == 1)
            {
                var parent = node.Parents.Single();
                scope = parent.Scope;

                Debug.Assert(scope != null);
                if (parent.Children.Count > 1)
                {
                    //Log("  One parent, but parent has many children so I inherit its scope by cloning");

                    // We follow two branches, so each one gets its own copy.
                    scope = parent.Scope.Clone();
                }
                else
                {
                    scope = parent.Scope;
                }

                //Log("  One parent, one child, so I inherit its original scope");

                UpdateScopeSingleOrNoParent(deltas, scope, deletedServerPathToId);
            }
            else if (IsMerge(node))
            {
                //Log("... this is a merge node.");

                var mergeInto = node.Parents[0];
                var mergeFrom = node.Parents[1];

                // Log($"Parents: {node.Parents[0].CommitHash} {node.Parents[1].CommitHash}");
                //Log("I try to resolve the merge before processing the item list.");

                scope = mergeInto.Scope;

                UpdateScopeTwoParents(deltas, mergeInto.Scope, mergeFrom.Scope, deletedServerPathToId);
            }
            else
            {
                Debug.Assert(false);
            }

            return (scope, deletedServerPathToId);
        }


        private void UpdateScopeSingleOrNoParent(Differences deltas, Scope scope, Dictionary<string, string> deleted)
        {
            foreach (var change in deltas.ChangesInCommit)
            {
                // These are the changes done on the merge commit itself (fixing conflicts etc)
                ApplyChangesToScope(scope, change, deleted);
            }
        }

        private void UpdateScopeTwoParents(Differences deltas, Scope mergeIntoScope, Scope mergeFromScope,
            Dictionary<string, string> deleted)
        {
            foreach (var change in deltas.DiffExclusiveToParent1)
            {
                // These are the changes done on the feature branch. We merge them into the scope.
                UpdateScopeFromMergeSource(mergeIntoScope, mergeFromScope, change, deleted);
            }

            foreach (var change in deltas.ChangesInCommit)
            {
                // These are the changes done on the merge commit itself (fixing conflicts etc)
                ApplyChangesToScope(mergeIntoScope, change, deleted);
            }
        }

        private void UpdateScopeFromMergeSource(Scope mergeIntoScope, Scope mergeFromScope, TreeEntryChanges change,
            IDictionary<string, string> deleted)
        {
            // Merge changes from feature branch. Apply all Ids we can find without ambiguity.

            if (change.Status == ChangeKind.Added)
            {
                var idFrom = mergeFromScope.GetIdOrDefault(change.Path);
                var idInto = mergeIntoScope.GetIdOrDefault(change.Path);
                if (idFrom != null && idInto == null &&
                    mergeIntoScope.GetServerPathOrDefault(Guid.Parse(idFrom)) == null)
                {
                    // Take the Id from the (feature) branch where the file was added.

                    // File is known in "from scope" but not in "into scope" and the id in "from scope" is not in "into scope"
                    mergeIntoScope.MergeAdd(change.Path, Guid.Parse(idFrom));
                    return;
                }

                if (idFrom != null && idFrom == idInto &&
                    mergeIntoScope.GetServerPath(Guid.Parse(idInto)) == change.Path)
                {
                    // Nothing to update
                    return;
                }

                // In all other cases reset tracking
                mergeIntoScope.Remove(change.Path);
                mergeIntoScope.Add(change.Path);
                _stats.ResetRenameTrackingOnFile++;
                Warnings.Add(new WarningMessage("", "Reset tracking"));
            }
            else if (change.Status == ChangeKind.Modified)
            {
                // Id must be known in main branch.
                Debug.Assert(mergeIntoScope.IsKnown(change.Path));
            }
            else if (change.Status == ChangeKind.Deleted)
            {
                // Neither of the (verified against the tree) parent scopes know this file!?!

                var id = mergeIntoScope.GetIdOrDefault(change.Path);
                if (id != null)
                {
                    mergeIntoScope.Remove(change.Path);
                    deleted.Add(change.Path, id);
                }
            }
            else if (change.Status == ChangeKind.Renamed)
            {
                if (mergeIntoScope.IsKnown(change.OldPath) is false)
                {
                    mergeIntoScope.Remove(change.Path);
                    mergeIntoScope.Add(change.Path);
                }
                else
                {
                    mergeIntoScope.Update(change.OldPath, change.Path);
                }
            }
            else
            {
                Debug.Fail("Not handled!");
            }
        }

        private static void ApplyChangesToScope(Scope scope, TreeEntryChanges change,
            Dictionary<string, string> deleted)
        {
            // Single parent

            if (change.Status == ChangeKind.Added)
            {
                scope.Add(change.Path);
            }
            else if (change.Status == ChangeKind.Modified)
            {
                // Scope is not affected
            }
            else if (change.Status == ChangeKind.Deleted)
            {
                var id = scope.GetId(change.Path);
                scope.Remove(change.Path);
                deleted.Add(change.Path, id);
            }
            else if (change.Status == ChangeKind.Renamed)
            {
                scope.Update(change.OldPath, change.Path);
            }
            else
            {
                Debug.Fail("Not handled!");
            }
        }

        private ChangeItem CreateChangeItem(TreeEntryChanges change, Scope scope,
            Dictionary<string, string> deletedServerPathToId)
        {
            var item = new ChangeItem();
            item.Kind = ToChangeKind(change.Status);
            item.ServerPath = change.Path;
            item.FromServerPath = change.OldPath;
            item.LocalPath = _mapper.MapToLocalFile(change.Path);


            // Assign id
            item.Id = scope.GetIdOrDefault(change.Path);
            if (item.Id == null)
            {
                Debug.Assert(change.Status == ChangeKind.Deleted);
                item.Id = deletedServerPathToId[change.Path];
            }


            return item;
        }

        private static ChangeSet CreateChangeSet(Commit commit)
        {
            var cs = new ChangeSet();
            cs.Comment = commit.MessageShort;
            cs.Id = commit.Sha;
            cs.Committer = commit.Author.Name;
            cs.Date = commit.Author.When.LocalDateTime; // ToString("yyyy-MM-dd'T'HH:mm:ssK")); // Instead of s or o
            return cs;
        }

        /// <summary>
        ///     Calculates the difference in working tree to each parent
        /// </summary>
        private Differences CalculateDiffs(Repository repo, Commit commit)
        {
            var options = new CompareOptions();

            var parents = commit.Parents.ToArray();

            if (parents.Length == 0)
            {
                var diffToParent = new List<TreeEntryChanges>();
                foreach (var change in repo.Diff.Compare<TreeChanges>(null, commit.Tree, options))
                {
                    diffToParent.Add(change);
                }

                return new Differences(diffToParent);
            }

            if (parents.Length == 1)
            {
                var diffToParent = repo.Diff.Compare<TreeChanges>(parents[0].Tree, commit.Tree, options).ToList();
                return new Differences(diffToParent);
            }

            // Merge commit has two parents
            Debug.Assert(parents.Length == 2);

            var parentMergeInto = parents[0];
            var diffToParent1 = repo.Diff.Compare<TreeChanges>(parentMergeInto.Tree, commit.Tree, options).ToList();

            var parentMergeFrom = parents[1];
            var diffToParent2 = repo.Diff.Compare<TreeChanges>(parentMergeFrom.Tree, commit.Tree, options).ToList();

            return new Differences(diffToParent1, diffToParent2);
        }

        /// <summary>
        ///     Getting the graph alone is quite fast. For NUnit repository it is less than 300ms
        /// </summary>
        private Graph GetGraph(Repository repo)
        {
            var graph = new Graph();
            var head = repo.Head.Tip;

            var processed = new HashSet<string>();
            var queue = new Queue<Commit>();
            queue.Enqueue(head);
            while (queue.Any())
            {
                var commit = queue.Dequeue();
                graph.UpdateGraph(commit.Sha, commit.Parents.Select(p => p.Sha));

                foreach (var parent in commit.Parents)
                {
                    if (processed.Add(parent.Sha))
                    {
                        queue.Enqueue(parent);
                    }
                }
            }

            return graph;
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

        private void SaveHistory(ChangeSetHistory history)
        {
            var json = JsonConvert.SerializeObject(history, Formatting.Indented);
            File.WriteAllText(_historyFile, json, Encoding.UTF8);
        }


        private KindOfChange ToChangeKind(ChangeKind kind)
        {
            switch (kind)
            {
                case ChangeKind.Unmodified:
                    break;
                case ChangeKind.Added:
                    return KindOfChange.Add;
                case ChangeKind.Deleted:
                    return KindOfChange.Delete;
                case ChangeKind.Modified:
                    return KindOfChange.Edit;
                case ChangeKind.Renamed:
                    return KindOfChange.Rename;
                case ChangeKind.Copied:
                    return KindOfChange.Copy;
                case ChangeKind.Ignored:
                    break;
                case ChangeKind.Untracked:
                    break;
                case ChangeKind.TypeChanged:
                    break;
                case ChangeKind.Unreadable:
                    break;
                case ChangeKind.Conflicted:
                    break;
            }

            return KindOfChange.None;
        }
    }
}