using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Insight.Shared;
using Insight.Shared.Model;

using Newtonsoft.Json;

namespace Insight.GitProvider
{
    /// <summary>
    /// Provides higher level functions and queries on a git repository.
    /// 
    /// Problem:
    /// Git tracks content and does not have a unique Id for a file. I want to have a unique Id, however.
    /// 
    /// So I create a simplified commit history where each local file is tracked by a unique Id.
    /// This means I have to stop tracking a file as soon it gets the ancestor of multiple other files.
    /// In this case I can no longer track a unique Id.
    /// 
    /// Another key point is that git log for a single file by default is simplified.
    /// A commit for example that is deleted in one branch and modified later 
    /// in another branch before merged is simply removed.
    /// 
    /// Algorithm
    /// 1. Ask git for all tracked (local) files
    /// 2. For each file request the history. Each commit record has a single file
    ///    in it. I assign a unique id for the file. This history is already simplified by git(!)
    /// 3. Reconstruct a simplified change set history.
    ///    Because a file (server path) can be the common ancestor of many other files it is possible to find
    ///    changesets containing files with different Ids but same server path.
    /// 4. Remove all these items that represent shared history for more than one tracked file.
    /// 
    /// So I track all renaming of a file until the file starts sharing history with another file.
    /// This allows me to use unique file ids but not losing too much of the history.
    /// This approach is easy to handle but is very slow for larger repositories.
    /// </summary>
    public sealed class GitProviderFileByFile : GitProviderBase, ISourceControlProvider
    {
        readonly object _lockObj = new object();
        Graph _graph;

        /// <summary>
        /// Debugging!
        /// </summary>
        private Dictionary<string, string> _idToLocalFile;

        public static string GetClass()
        {
            var type = typeof(GitProviderFileByFile);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        public void Initialize(string projectBase, string cachePath, IFilter fileFilter, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;
            _fileFilter = fileFilter;

            _historyFile = Path.Combine(cachePath, "git_history.json");
            _contributionFile = Path.Combine(cachePath, "contribution.json");
            _gitCli = new GitCommandLine(_startDirectory);

            _mapper = new PathMapper(_startDirectory);
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

        public void UpdateCache(IProgress progress, bool includeWorkData)
        {
            DeleteAllCaches();

            VerifyGitPreConditions();
            PrepareLogDirectory();

            UpdateHistory(progress);

            if (includeWorkData)
            {
                // Optional
                UpdateContribution(progress);
            }
        }

        void PrepareLogDirectory()
        {
            var logPath = Path.Combine(_cachePath, "logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
        }

        /// <summary>
        /// file Id -> change set ids where to remove the file
        /// </summary>
        static Dictionary<string, HashSet<string>> FindSharedHistory(List<ChangeSet> historyOrderedByDate)
        {
            // file id -> change set id
            var filesToRemove = new Dictionary<string, HashSet<string>>();

            // Find overlapping files in history.
            foreach (var cs in historyOrderedByDate)
            {
                var serverPaths = new HashSet<string>();
                var duplicateServerPaths = new HashSet<string>();

                // Pass 1: Find which server paths are used more than once in the current changeset
                foreach (var item in cs.Items)
                {
                    // Remember the files to be deleted.
                    if (!serverPaths.Add(item.ServerPath))
                    {
                        // Same server path on more than one change set items.
                        duplicateServerPaths.Add(item.ServerPath);
                    }
                }

                // Pass 2: Determine the files to be deleted.
                foreach (var item in cs.Items)
                {
                    // Since we deal with a graph we may have to clean several branches later.
                    if (duplicateServerPaths.Contains(item.ServerPath))
                    {
                        // Remove the file with id=item.Id starting from change set cs.Id
                        if (!filesToRemove.ContainsKey(item.Id))
                        {
                            filesToRemove.Add(item.Id, new HashSet<string> { cs.Id });
                        }
                        else
                        {
                            filesToRemove[item.Id].Add(cs.Id);
                        }
                    }
                }
            }

            return filesToRemove;
        }


        // ReSharper disable once UnusedMember.Local
        void DebugWriteLogForSingleFile(string gitFileLog, string forLocalFile)
        {
            var logPath = Path.Combine(_cachePath, "logs");          

            var file = forLocalFile.Replace("\\", "_").Replace(":", "_");
            file = Path.Combine(logPath, file);

            // Ensure same file stored in different directories don't override each other.
            File.WriteAllText(file, gitFileLog);
        }

        void ProcessHistoryForFile(string localPath, Dictionary<string, ChangeSet> history)
        {
            var id = Guid.NewGuid().ToString();
            _idToLocalFile.Add(id, localPath);

            var gitFileLog = _gitCli.Log(localPath);
            
            // Writes logs for all files (debugging)
            //DebugWriteLogForSingleFile(gitFileLog, localPath);

            var parser = new Parser(_mapper);
            parser.WorkItemRegex = _workItemRegex;

            var (fileHistory, _) = parser.ParseLogString(gitFileLog);

            foreach (var cs in fileHistory.ChangeSets)
            {
                Debug.Assert(_graph.Exists(cs.Id));
                var singleFile = cs.Items.Single();

                if (singleFile.Kind == KindOfChange.Delete)
                {
                    // File was deleted and maybe added later again.
                    // Stop following this file. Do not add current version to
                    // history. Analyzer.LoadHistory will kill all deleted items
                    // and the file will not be part of the summary anyway.
                    break;
                }

                lock (_lockObj)
                {
                    if (!history.ContainsKey(cs.Id))
                    {
                        singleFile.Id = id;
                        history.Add(cs.Id, cs);
                    }
                    else
                    {
                        // Seen this change set before. Add the file.
                        var changeSet = history[cs.Id];
                        singleFile.Id = id;
                        changeSet.Items.Add(singleFile);
                    }

                    // Convert copy to add
                    if (singleFile.Kind == KindOfChange.Copy)
                    {
                        // Consider copy as a new start. Ignore anything before
                        // Example: 
                        // StringId is created in cs1. Not touched later.
                        // NumberId is copied from StringId in cs2. Not touched later.
                        // Shared history in cs1 would cause StringId to disappear.
                        singleFile.Kind = KindOfChange.Add;
                        break;
                    }
                }
            }
        }

        List<ChangeSet> RebuildHistory(List<string> localPaths, IProgress progress)
        {
            var count = localPaths.Count;
            var counter = 0;

            // hash -> cs
            var history = new Dictionary<string, ChangeSet>();

            Parallel.ForEach(localPaths,
                             localPath =>
                             {
                                 // Progress
                                 Interlocked.Increment(ref counter);
                                 progress.Message($"Rebuilding history {counter}/{count}");

                                 ProcessHistoryForFile(localPath, history);
                             });

            return history.Values.OrderByDescending(x => x.Date).ToList();
        }

        void SaveFullLogToDisk()
        {
            var log = _gitCli.Log();
            File.WriteAllText(Path.Combine(_cachePath, @"git_full_history.txt"), log);
        }

        void SaveRecoveredLogToDisk(List<ChangeSet> commits, Graph graph)
        {
            Dump(Path.Combine(_cachePath, "git_recovered_history.txt"), commits, graph);
        }

        
        void UpdateHistory(IProgress progress)
        {            
            // Git graph
            _graph = new Graph();
            _idToLocalFile = new Dictionary<string, string>();

            
            // Build the full graph
            var fullLog = _gitCli.Log();
            var parser = new Parser(_mapper);
            var (_, graph) = parser.ParseLogString(fullLog);
            _graph = graph;

            // Save the original log for information (debugging)
            //SaveFullLogToDisk();

            // Build a virtual commit history
            var localPaths = GetAllTrackedLocalFiles();
         
            var history = RebuildHistory(localPaths, progress);

            // file id -> List of change set id
            var filesToRemove = FindSharedHistory(history);

            // Cleanup history. We stop tracking a file if it starts sharing its history with another file.
            // The history may skip some commits so we use the full graph to traverse everything.
            DeleteSharedHistory(history, filesToRemove);

            // Write history file
            var json = JsonConvert.SerializeObject(new ChangeSetHistory(history), Formatting.Indented);
            File.WriteAllText(_historyFile, json, Encoding.UTF8);

            // Save the constructed log for information
            SaveRecoveredLogToDisk(history, _graph);

            // Just a plausibility check
            VerifyNoDuplicateServerPathsInChangeset(history);
        }

        /// <summary>
        /// Empty merge commits are removed implicitly
        /// In each commit remove the files that are ancestors for more than one file.
        /// For each file to remove we traverse the whole graph from the starting commit.
        /// </summary>
        public void DeleteSharedHistory(List<ChangeSet> historyToModify, Dictionary<string, HashSet<string>> filesToRemove)
        {
            lock (_lockObj)
            {
                DeleteSharedHistory(historyToModify, filesToRemove, _graph);
            }
        }

        public static void DeleteSharedHistory(List<ChangeSet> historyToModify, Dictionary<string, HashSet<string>> filesToRemove, Graph graph)
        {
             // filesToRemove: 
                // fileId -> commit hash (change set id) where we start removing the file
                var idToChangeSet = historyToModify.ToDictionary(x => x.Id, x => x);


                foreach (var fileToRemove in filesToRemove)
                {
                    var fileIdToRemove = fileToRemove.Key;
                    var changeSetIds = fileToRemove.Value;

                    // Traverse graph to find all change sets where we have to delete the files
                    // Note: The simplified history is incomplete.
                    var nodesToProcess = new Queue<GraphNode>();
                    var handledNodes = new HashSet<string>();
                    GraphNode node;

                    foreach (var csId in changeSetIds)
                    {
                        if (graph.TryGetNode(csId, out node))
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

                        if (idToChangeSet.ContainsKey(node.CommitHash))
                        {
                            // Note: The history of a file may skip some commits if they are not relevant.
                            // Therefore it is possible that no changeset exists for the hash.
                            // Remember that we follow the full graph.
                            var cs = idToChangeSet[node.CommitHash];

                            // Remove the file from change set
                            cs.Items.RemoveAll(i => i.Id == fileIdToRemove);
                        }

                        // Delete the current file also in all parents
                        foreach (var parent in node.Parents)
                        {
                            var parentHash = parent.CommitHash;
                            // Avoid cycles in case a change set is parent of many others.
                            if (!handledNodes.Contains(parentHash) && graph.TryGetNode(parentHash, out node))
                            {
                                nodesToProcess.Enqueue(node);
                                handledNodes.Add(node.CommitHash);
                            }
                        }
                    }
                }
        }


        private void VerifyNoDuplicateServerPathsInChangeset(List<ChangeSet> commits)
        {
            foreach (var cs in commits)
            {
                try
                {
                    cs.Items.ToDictionary(k => k.ServerPath, k => k.ServerPath);
                }
                catch(Exception)
                {
                    foreach (var item in cs.Items)
                    {
                        Debug.WriteLine(item.Id + " -> " + item.LocalPath + "-> " + _idToLocalFile[item.Id]);
                    }
                    throw;
                }
            }
        }
    }
}