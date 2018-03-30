using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;

using Newtonsoft.Json;

namespace Insight.GitProvider
{
    /// <summary>
    /// Provides higher level funtions and queries on a git repository.
    /// </summary>
    public sealed class GitProvider : ISourceControlProvider
    {
        static readonly Regex _regex = new Regex(@"\\(?<Value>[a-zA-Z0-9]{3})", RegexOptions.Compiled);
        readonly string endHeaderMarker = "END_HEADER";

        readonly string recordMarker = "START_HEADER";
        string _cachePath;
        IFilter _fileFilter;
        GitCommandLine _gitCli;
        string _gitHistoryExportFile;


        Dictionary<string, GraphNode> _graph = new Dictionary<string, GraphNode>();

        string _lastLine;
        string _startDirectory;
        string _workItemRegex;

        public List<WarningMessage> Warnings { get; private set; }

        public static string GetClass()
        {
            var type = typeof(GitProvider);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        public Dictionary<string, uint> CalculateDeveloperWork(Artifact artifact)
        {
            var annotate = _gitCli.Annotate(artifact.LocalPath);

            //S = not a whitespace
            //s = whitespace

            // Parse annotated file
            var workByDevelopers = new Dictionary<string, uint>();
            var changeSetRegex = new Regex(@"^\S+\t\(\s*(?<developerName>[^\t]+).*", RegexOptions.Multiline | RegexOptions.Compiled);

            // Work by changesets (line by line)
            var matches = changeSetRegex.Matches(annotate);
            foreach (Match match in matches)
            {
                var developer = match.Groups["developerName"].Value;
                developer = developer.Trim('\t');
                workByDevelopers.AddToValue(developer, 1);
            }

            return workByDevelopers;
        }

        // TODO that seems unreliable
        public string Decoder(string value)
        {
            var replace = _regex.Replace(
                                         value,
                                         m => ((char) int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString()
                                        );
            return replace.Trim('"');
        }

        public List<FileRevision> ExportFileHistory(string localFile)
        {
            var result = new List<FileRevision>();

            var xml = _gitCli.Log(localFile);

            var historyOfSingleFile = ParseLogString(xml);
            foreach (var cs in historyOfSingleFile.ChangeSets)
            {
                var changeItem = cs.Items.First();

                var fi = new FileInfo(localFile);
                var exportFile = GetPathToExportedFile(fi, cs.Id);

                // Download if not already in cache
                if (!File.Exists(exportFile))
                {
                    _gitCli.ExportFileRevision(changeItem.ServerPath, cs.Id, exportFile);
                }

                var revision = new FileRevision(changeItem.LocalPath, cs.Id, cs.Date, exportFile);
                result.Add(revision);
            }

            return result;
        }

        public HashSet<string> GetAllTrackedFiles()
        {
            var serverPaths = _gitCli.GetAllTrackedFiles();
            var all = serverPaths.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(all);
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
            if (!File.Exists(_gitHistoryExportFile))
            {
                var msg = $"Log export file '{_gitHistoryExportFile}' not found. You have to 'Sync' first.";
                throw new FileNotFoundException(msg);
            }

            var json = File.ReadAllText(_gitHistoryExportFile, Encoding.UTF8);
            return JsonConvert.DeserializeObject<ChangeSetHistory>(json);
        }


        public void UpdateCache(IProgress progress)
        {
            CheckIsGitDirectory();


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

        private void SaveRecoveredLogToDisk(List<ChangeSet> commits)
        {
            Dump(Path.Combine(_cachePath, "git_recovered_history.txt"), commits, _graph);
        }

        private void SaveFullLogToDisk()
        {
            var log = _gitCli.Log();
            File.WriteAllText(Path.Combine(_cachePath, @"git_full_history.txt"), log);
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
        /// I don't want to run into merge conflicts.
        /// Abort if there are local changes to the working or staging area.
        /// Abort if there are local commits not pushed to the remote.
        /// </summary>
        void AbortOnPotentialMergeConflicts()
        {
            if (_gitCli.HasLocalChanges())
            {
                throw new Exception("Abort. There are local changes.");
            }

            if (_gitCli.HasLocalCommits())
            {
                throw new Exception("Abort. There are local commits.");
            }
        }

        void CheckIsGitDirectory()
        {
            if (!Directory.Exists(Path.Combine(_startDirectory, ".git")))
            {
                // We need the root (containing .git) because of the function MapToLocalFile.
                // We could go upwards and find the git root ourself and use this root for the path mapping.
                // But for the moment take everything. The user can set filters in the project settings.
                throw new ArgumentException("The given start directory is not the root of a git repository.");
            }
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
                ci.FromServerPath = oldName;
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

      

        List<string> GetAllTrackedLocalFiltes()
        {
            var trackedServerPaths = GetAllTrackedFiles();

            // Filtered local paths
            return trackedServerPaths.Select(sp => MapToLocalFile(sp))
                                     .Where(lp => _fileFilter.IsAccepted(lp))
                                     .ToList();
        }


        string GetHistoryCache()
        {
            var path = Path.Combine(_cachePath, "History");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        string GetPathToExportedFile(FileInfo localFile, string revision)
        {
            var name = new StringBuilder();

            name.Append(localFile.FullName.GetHashCode().ToString("X"));
            name.Append("_");
            name.Append(revision);
            name.Append("_");
            name.Append(localFile.Name);

            return Path.Combine(GetHistoryCache(), name.ToString());
        }

        bool GoToNextRecord(StreamReader reader)
        {
            if (_lastLine == recordMarker)
            {
                // We are already positioned on the next changeset.
                return true;
            }

            string line;
            while ((line = ReadLine(reader)) != null)
            {
                if (line.Equals(recordMarker))
                {
                    return true;
                }
            }

            return false;
        }

        string MapToLocalFile(string serverPath)
        {
            // In git we have the restriction 
            // that we cannot choose any sub directory.
            // (Current knowledge). Select the one with .git for the moment.

            // Example
            // _startDirectory = d:\\....\Insight
            // serverPath = Insight/Board.txt
            // localPath = d:\\....\Insight\Insight/Board.txt
            var serverNormalized = serverPath.Replace("/", "\\");
            var localPath = Path.Combine(_startDirectory, serverNormalized);
            return localPath;
        }

        /// <summary>
        /// Log file has format specified in GitCommandLine class
        /// </summary>
        ChangeSetHistory ParseLog(Stream log)
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

        ChangeSetHistory ParseLogFile(string logFile)
        {
            using (var stream = new FileStream(logFile, FileMode.Open))
            {
                var history = ParseLog(stream);
                return history;
            }
        }

        ChangeSetHistory ParseLogString(string logString)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(logString));
            return ParseLog(stream);
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

        void ParseWorkItemsFromComment(List<WorkItem> workItems, string comment)
        {
            if (!string.IsNullOrEmpty(_workItemRegex))
            {
                var extractor = new WorkItemExtractor(_workItemRegex);
                workItems.AddRange(extractor.Extract(comment));
            }
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

        string ReadComment(StreamReader reader)
        {
            string commentLine;

            var commentBuilder = new StringBuilder();
            while ((commentLine = ReadLine(reader)) != endHeaderMarker)
            {
                if (!string.IsNullOrEmpty(commentLine))
                {
                    commentBuilder.AppendLine(commentLine);
                }
            }

            Debug.Assert(commentLine == endHeaderMarker);
            return commentBuilder.ToString().Trim('\r', '\n');
        }

        string ReadLine(StreamReader reader)
        {
            // The only place where we read
            _lastLine = reader.ReadLine()?.Trim();
            return _lastLine;
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

                //File.WriteAllText(Path.Combine(_cachePath, "logs", new FileInfo(localPath).Name), fileLog);
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

        KindOfChange ToKindOfChange(string kind)
        {
            if (kind.StartsWith("R"))
            {
                // Followed by the similarity
                return KindOfChange.Rename;
            }

            if (kind.StartsWith("C"))
            {
                // Followed by the similarity.              
                return KindOfChange.Copy;
            }
            else if (kind == "A")
            {
                return KindOfChange.Add;
            }
            else if (kind == "D")
            {
                return KindOfChange.Delete;
            }
            else if (kind == "M")
            {
                return KindOfChange.Edit;
            }
            else
            {
                Debug.Assert(false);
                return KindOfChange.None;
            }
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