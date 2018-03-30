using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Insight.Shared;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;

using Newtonsoft.Json;

namespace Insight.GitProvider
{
    /// <summary>
    /// Provides higher level funtions and queries on a git repository.
    /// </summary>
    public sealed class GitProvider : GitProviderBase, ISourceControlProvider
    {
        Dictionary<string, GraphNode> _graph = new Dictionary<string, GraphNode>();

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

            _gitHistoryExportFile = Path.Combine(cachePath, @"git_history.json");
            _gitCli = new GitCommandLine(_startDirectory);
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

        /// <summary>
        /// Log file has format specified in GitCommandLine class
        /// </summary>
        protected override ChangeSetHistory ParseLog(Stream log)
        {
            var changeSets = new List<ChangeSet>();
            var tracker = new MovementTracker();

            using (var reader = new StreamReader(log))
            {
                var proceed = GoToNextRecord(reader);
                if (!proceed)
                {
                    throw new FormatException("The file does not contain any change sets.");
                }

                while (proceed)
                {
                    var changeSet = ParseRecord(reader);
                    changeSets.Add(changeSet);
                    proceed = GoToNextRecord(reader);
                }
            }

            Warnings = tracker.Warnings;
            var history = new ChangeSetHistory(changeSets.OrderByDescending(x => x.Date).ToList());
            return history;
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

                // TODO is it different with this code?
                //// Pass 0: Just convert copy to add.
                //foreach (var item in cs.Items)
                //{
                //    // Consider copy as a new start.
                //    if (item.Kind == KindOfChange.Copy)
                //    {
                //        item.Kind = KindOfChange.Add;

                //        if (_graph.ContainsKey(cs.Id))
                //        {
                //            // Parent exists
                //            foreach (var parent in _graph[item.Id].Parents)
                //            {
                //                // We already know to skip any further occurences of this file.
                //                filesToRemove.Add(item.Id, parent);
                //            }
                //        }
                //    }
                //}

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

        void CreateChangeItem(ChangeSet cs, string changeItem)
        {
            var ci = new ChangeItem();

            // Example
            // M Visualization.Controls/Strings.resx
            // A Visualization.Controls/Tools/IHighlighting.cs
            // R083 Visualization.Controls/Filter/FilterView.xaml   Visualization.Controls/Tools/ToolView.xaml

            var parts = changeItem.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var changeKind = ToKindOfChange(parts[0]);
            ci.Kind = changeKind;

            if (changeKind == KindOfChange.Rename || changeKind == KindOfChange.Copy)
            {
                Debug.Assert(parts.Length == 3);
                var oldName = parts[1];
                var newName = parts[2];
                ci.ServerPath = Decoder(newName);
                ci.FromServerPath = Decoder(oldName);
                cs.Items.Add(ci);
            }
            else
            {
                Debug.Assert(parts.Length == 2 || parts.Length == 3);
                ci.ServerPath = Decoder(parts[1]);
                cs.Items.Add(ci);
            }

            ci.LocalPath = MapToLocalFile(ci.ServerPath);
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


        ChangeSet ParseRecord(StreamReader reader)
        {
            // We are located on the first data item of the record
            var hash = ReadLine(reader);
            var committer = ReadLine(reader);
            var date = ReadLine(reader);
            var parents = ReadLine(reader);

            var comment = ReadComment(reader);

            UpdateGraph(hash, parents);

            var cs = new ChangeSet();
            cs.Id = hash;
            cs.Committer = committer;
            cs.Comment = comment;

            ParseWorkItemsFromComment(cs.WorkItems, cs.Comment);

            cs.Date = DateTime.Parse(date);

            ReadChangeItems(cs, reader);
            return cs;
        }


        void ReadChangeItems(ChangeSet cs, StreamReader reader)
        {
            // Now parse the files!
            var changeItem = ReadLine(reader);
            while (changeItem != null && changeItem != recordMarker)
            {
                if (!string.IsNullOrEmpty(changeItem))
                {
                    CreateChangeItem(cs, changeItem);
                }

                changeItem = ReadLine(reader);
            }
        }


        List<ChangeSet> RebuildHistory(List<string> localPaths, IProgress progress)
        {
            var count = localPaths.Count;
            var counter = 0;

            var debug = new Dictionary<string, string>();

            // id -> cs
            var commits = new Dictionary<string, ChangeSet>();
            foreach (var localPath in localPaths)
            {
                counter++;
                progress.Message($"Rebuilding history {counter}/{count}");

                var id = Guid.NewGuid().ToString();

                var fileLog = _gitCli.Log(localPath);

                File.WriteAllText(Path.Combine(_cachePath, "logs", new FileInfo(localPath).Name), fileLog);
                var fileHistory = ParseLogString(fileLog);

                foreach (var cs in fileHistory.ChangeSets)
                {
                    var singleFile = cs.Items.Single();
                    if (!debug.ContainsKey(id))
                    {
                        debug.Add(id, localPath); // which id belonged to which item?
                    }

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
                }
            }

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

            if (!_graph.ContainsKey(node.Commit))
            {
                _graph.Add(node.Commit, node);
            }
        }
    }
}