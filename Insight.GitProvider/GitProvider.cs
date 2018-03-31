using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Insight.Shared;
using Insight.Shared.Model;

using Newtonsoft.Json;

namespace Insight.GitProvider
{
    /// <summary>
    /// Provides higher level funtions and queries on a git repository.
    /// </summary>
    public sealed class GitProvider : GitProviderBase, ISourceControlProvider
    {
        Dictionary<string, GraphNode> _graph = new Dictionary<string, GraphNode>();
        readonly object _lockObj = new object();
        PathMapper _mapper;

        public static string GetClass()
        {
            var type = typeof(GitProvider);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }


        public void Initialize(string projectBase, string cachePath, IFilter fileFilter, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;
            _fileFilter = fileFilter;

            _gitHistoryExportFile = Path.Combine(cachePath, "git_history.json");
            _gitCli = new GitCommandLine(_startDirectory);

            _mapper = new PathMapper(_startDirectory);
        }

        /// <summary>
        /// You need to call UpdateCache before.
        /// </summary>
        public ChangeSetHistory QueryChangeSetHistory()
        {
            VerifyHistoryIsCached();

            var json = File.ReadAllText(_gitHistoryExportFile, Encoding.UTF8);
            return JsonConvert.DeserializeObject<ChangeSetHistory>(json);
        }


        public void UpdateCache(IProgress progress)
        {
            VerifyGitDirectory();

            // Git graph
            _graph = new Dictionary<string, GraphNode>();

            // Build a virtual commit history
            var localPaths = GetAllTrackedLocalFiltes();

            // sha1 -> commit
            var commits = RebuildHistory(localPaths, progress);

            // file id -> change set id
            var filesToRemove = FindSharedHistory(commits);

            // Cleanup history. We stop tracking a file if it starts sharing its history with another file.
            CleanupHistory(commits, filesToRemove);

            // Write history file
            var json = JsonConvert.SerializeObject(new ChangeSetHistory(commits), Formatting.Indented);
            File.WriteAllText(_gitHistoryExportFile, json, Encoding.UTF8);

            // Save the original log for information 
            SaveFullLogToDisk();

            // Save the constructed log for information
            SaveRecoveredLogToDisk(commits);
        }     

        static Dictionary<string, string> FindSharedHistory(List<ChangeSet> commits)
        {
            // file id -> change set id
            var filesToRemove = new Dictionary<string, string>();

            // Find overlapping files in history.
            foreach (var cs in commits)
            {
                // Commits are ordered by date

                // Server path

                var serverPaths = new HashSet<string>();
                var duplicateServerPaths = new HashSet<string>();

                // Pass 1: Find which server paths are used more than once in the change set
                foreach (var item in cs.Items)
                {
                    // Second pass: Remember the files to be deleted.
                    if (!serverPaths.Add(item.ServerPath))
                    {
                        // Same server path on more than one change set items.
                        duplicateServerPaths.Add(item.ServerPath);
                    }
                }

                // Pass 2: Determine the files to be deleted.
                foreach (var item in cs.Items)
                {
                    if (duplicateServerPaths.Contains(item.ServerPath))
                    {
                        if (!filesToRemove.ContainsKey(item.Id))
                        {
                            filesToRemove.Add(item.Id, cs.Id);
                        }
                    }
                }
            }

            return filesToRemove;
        }


        /// <summary>
        /// Empty merge commits are removed implicitely
        /// </summary>
        void CleanupHistory(List<ChangeSet> commits, Dictionary<string, string> filesToRemove)
        {
            // filesToRemove: id -> cs
            var lookup = commits.ToDictionary(x => x.Id, x => x);
            foreach (var fileToRemove in filesToRemove)
            {
                var fileId = fileToRemove.Key;
                var changeSetId = fileToRemove.Value;

                // Traverse graph to find all changesets where we have to delete the files
                var nodesToProcess = new Queue<GraphNode>();
                GraphNode node;
                if (_graph.TryGetValue(changeSetId, out node))
                {
                    nodesToProcess.Enqueue(node);
                }

                while (nodesToProcess.Any())
                {
                    // Remove the file from change set
                    node = nodesToProcess.Dequeue();
                    var cs = lookup[changeSetId];
                    cs.Items.RemoveAll(i => i.Id == fileId);

                    foreach (var parent in node.Parents)
                    {
                        if (_graph.TryGetValue(parent, out node))
                        {
                            nodesToProcess.Enqueue(node);
                        }
                    }
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        void DebugWriteLogForSingleFile(string gitFileLog, string forLocalFile)
        {
            var logPath = Path.Combine(_cachePath, "logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            File.WriteAllText(Path.Combine(logPath, new FileInfo(forLocalFile).Name), gitFileLog);
        }


        void Dump(string path, List<ChangeSet> commits, Dictionary<string, GraphNode> graph)
        {
            // For debugging
            var writer = new StreamWriter(path);
            foreach (var commit in commits)
            {
                writer.WriteLine("START_HEADER");
                writer.WriteLine(commit.Id);
                writer.WriteLine(commit.Committer);
                writer.WriteLine(commit.Date.ToString("o"));
                writer.WriteLine(string.Join("\t", graph[commit.Id].Parents));
                writer.WriteLine(commit.Comment);
                writer.WriteLine("END_HEADER");

                // files
                foreach (var file in commit.Items)
                {
                    switch (file.Kind)
                    {
                        // Lose the similarity
                        case KindOfChange.Add:
                            writer.WriteLine("A\t" + file.ServerPath);
                            break;
                        case KindOfChange.Edit:
                            writer.WriteLine("M\t" + file.ServerPath);
                            break;
                        case KindOfChange.Copy:
                            writer.WriteLine("C\t" + file.FromServerPath + "\t" + file.ServerPath);
                            break;
                        case KindOfChange.Rename:
                            writer.WriteLine("R\t" + file.FromServerPath + "\t" + file.ServerPath);
                            break;
                    }
                }
            }
        }

        List<string> GetAllTrackedLocalFiltes()
        {
            var trackedServerPaths = GetAllTrackedFiles();

            // Filtered local paths
            return trackedServerPaths.Select(sp => _mapper.MapToLocalFile(sp))
                                     .Where(lp => _fileFilter.IsAccepted(lp))
                                     .ToList();
        }

        void ProcessHistoryForFile(string localPath, Dictionary<string, ChangeSet> commits)
        {
            var id = Guid.NewGuid().ToString();

            var gitLogString = _gitCli.Log(localPath);

            //DebugWriteLogForSingleFile(gitFileLog, localPath);

            var parser = new Parser(_mapper, UpdateGraph);
            parser.WorkItemRegex = _workItemRegex;

            var fileHistory = parser.ParseLogString(gitLogString);

            foreach (var cs in fileHistory.ChangeSets)
            {
                var singleFile = cs.Items.Single();

                lock (_lockObj)
                {
                    if (!commits.ContainsKey(cs.Id))
                    {
                        singleFile.Id = id;
                        commits.Add(cs.Id, cs);
                    }
                    else
                    {
                        // Seen this changeset before. Add the file.
                        var changeSet = commits[cs.Id];
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

            // id -> cs
            var commits = new Dictionary<string, ChangeSet>();

            Parallel.ForEach(localPaths, localPath =>
                                         {
                                             // Progress
                                             Interlocked.Increment(ref counter);
                                             progress.Message($"Rebuilding history {counter}/{count}");

                                             ProcessHistoryForFile(localPath, commits);
                                         });

            return commits.Values.OrderByDescending(x => x.Date).ToList();
        }

        void SaveFullLogToDisk()
        {
            var log = _gitCli.Log();
            File.WriteAllText(Path.Combine(_cachePath, @"git_full_history.txt"), log);
        }

        void SaveRecoveredLogToDisk(List<ChangeSet> commits)
        {
            Dump(Path.Combine(_cachePath, "git_recovered_history.txt"), commits, _graph);
        }

        void UpdateGraph(string hash, string parents)
        {
            var allParents = parents.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(parent => parent)
                                    .ToList();
            var node = new GraphNode { Commit = hash, Parents = allParents };

            lock (_lockObj)
            {
                if (!_graph.ContainsKey(node.Commit))
                {
                    _graph.Add(node.Commit, node);
                }
            }
        }
    }
}