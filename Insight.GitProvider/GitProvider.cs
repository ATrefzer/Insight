using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

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
        private static readonly Regex _regex = new Regex(@"\\(?<Value>[a-zA-Z0-9]{3})", RegexOptions.Compiled);
        private readonly string endHeaderMarker = "END_HEADER";

        private readonly string recordMarker = "START_HEADER";
        private string _cachePath;
        private GitCommandLine _gitCli;
        private string _gitHistoryExportFile;

        private string _lastLine;
        private string _startDirectory;
        private string _workItemRegex;

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
                                         m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString()
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

        public void Initialize(string projectBase, string cachePath, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;

            _gitHistoryExportFile = Path.Combine(cachePath, @"git_history.log");
            _gitCli = new GitCommandLine(_startDirectory);
        }

        public List<WarningMessage> Warnings { get; private set; }

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
         
            // Read pre parsed history
            var file = new BinaryFile<List<ChangeSet>>();
            var list = file.Read(_gitHistoryExportFile);
            return new ChangeSetHistory(list);
        }


        class GraphNode
        {
            public Id Commit { get; set; }
            public List<Id> Parents { get; set; }
        }


        public void UpdateCache()
        {

            if (!Directory.Exists(Path.Combine(_startDirectory, ".git")))
            {
                // We need the root (containing .git) because of the function MapToLocalFile.
                // We could go upwards and find the git root ourself and use this root for the path mapping.
                // But for the moment take everything. The user can set filters in the project settings.
                throw new ArgumentException("The given start directory is not the root of a git repository.");
            }

            var fullLog = _gitCli.Log();
            File.WriteAllText(_gitHistoryExportFile + ".full.txt", fullLog);

            // TODO atr
            var filter = new ExtensionIncludeFilter(".cs");

            // TODO
            // Build a virtual commit history
            var trackedServerPaths = GetAllTrackedFiles();

            // Filtered local paths
            var localPaths = trackedServerPaths.Select(sp => MapToLocalFile(sp)).Where(lp => filter.IsAccepted(lp)).ToList();

            // Git graph
            var debug = new Dictionary<Id, ChangeItem>();
            _graph = new Dictionary<Id, GraphNode>();

            /// Rules: Find a copy? stop there and treat it like an add.

            // sha1 -> commit
            var commits = new Dictionary<Id, ChangeSet>();
            foreach (var localPath in localPaths)
            {
                var id = new StringId(Guid.NewGuid().ToString());

                var fileLog = _gitCli.Log(localPath);
                var fileHistory = ParseLogString(fileLog);

                foreach (var cs in fileHistory.ChangeSets)
                {
                    var singleFile = cs.Items.Single();
                    if (!debug.ContainsKey(id))
                        debug.Add(id, singleFile); // which id belonged to which item?


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

                        // Shared history?
                        Debug.Assert(changeSet.Items.Any(x => x.ServerPath == singleFile.ServerPath) == false);
                        changeSet.Items.Add(singleFile);


                    }

                    if (singleFile.Kind == KindOfChange.Copy)
                    {
                        // Stop tracking this file. Consider copy as a new start.
                        singleFile.Kind = KindOfChange.Add;
                        break;
                    }
                }
            }

            var ordered = commits.Values.OrderByDescending(x => x.Date).ToList();

            // Cleanup the history
            foreach (var cs in ordered)
            {

                var sp = new HashSet<string>();
                var lp = new HashSet<string>();
                foreach (var a in cs.Items)
                {
                        Debug.Assert(sp.Add(a.ServerPath));

                    //  follow change set id in graph and remove both Ids!
                    // starting in this change set!


                    // In same change set we have a copy of another file!
                    // Add in change set this but remove both ids in all older changesets
                    //if (a.FromServerPath != null)
                    //    Debug.Assert(lp.Add(a.FromServerPath));
                }

                var file = new BinaryFile<List<ChangeSet>>();
                file.Write(_gitHistoryExportFile, ordered);
            }
        }

        /// <summary>
        /// I don't want to run into merge conflicts.
        /// Abort if there are local changes to the working or staging area.
        /// Abort if there are local commits not pushed to the remote.
        /// </summary>
        private void AbortOnPotentialMergeConflicts()
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

        private void CreateChangeItem(ChangeSet cs, string changeItem)
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


        private string GetHistoryCache()
        {
            var path = Path.Combine(_cachePath, "History");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        private string GetPathToExportedFile(FileInfo localFile, Id revision)
        {
            var name = new StringBuilder();

            name.Append(localFile.FullName.GetHashCode().ToString("X"));
            name.Append("_");
            name.Append(revision);
            name.Append("_");
            name.Append(localFile.Name);

            return Path.Combine(GetHistoryCache(), name.ToString());
        }

        private bool GoToNextRecord(StreamReader reader)
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

        private string MapToLocalFile(string serverPath)
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
        private ChangeSetHistory ParseLog(Stream log)
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

        public HashSet<string> GetAllTrackedFiles()
        {
            var serverPaths = _gitCli.GetAllTrackedFiles();
            var all = serverPaths.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(all);
        }

        private ChangeSetHistory ParseLogFile(string logFile)
        {
            using (var stream = new FileStream(logFile, FileMode.Open))
            {
                var history = ParseLog(stream);           
                return history;
            }
        }

        private ChangeSetHistory ParseLogString(string logString)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(logString));
            return ParseLog(stream);
        }


        Dictionary<Id, GraphNode> _graph = new Dictionary<Id, GraphNode>();

        private ChangeSet ParseRecord(StreamReader reader)
        {
            // We are located on the first data item of the record
            var hash = ReadLine(reader);
            var committer = ReadLine(reader);
            var date = ReadLine(reader);

            // Git graph
            var parents = ReadLine(reader);
            UpdateGraph(hash, parents);

            var commentBuilder = new StringBuilder();
            string commentLine;

            while ((commentLine = ReadLine(reader)) != endHeaderMarker)
            {
                if (!string.IsNullOrEmpty(commentLine))
                {
                    commentBuilder.AppendLine(commentLine);
                }
            }

            var cs = new ChangeSet();
            cs.Id = new StringId(hash);
            cs.Committer = committer;
            cs.Comment = commentBuilder.ToString().Trim('\r', '\n');

            ParseWorkItemsFromComment(cs.WorkItems, cs.Comment);

            cs.Date = DateTime.Parse(date);

            Debug.Assert(commentLine == endHeaderMarker);

            ReadChangeItems(cs, reader);
            return cs;
        }

        private void UpdateGraph(string hash, string parents)
        {
            var allParents = parents.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(p => new StringId(p) as Id)
                                                .ToList();
            var node = new GraphNode { Commit = new StringId(hash), Parents = allParents };

            if (!_graph.ContainsKey(node.Commit))
                _graph.Add(node.Commit, node);
            
        }

        private void ParseWorkItemsFromComment(List<WorkItem> workItems, string comment)
        {
            if (!string.IsNullOrEmpty(_workItemRegex))
            {
                var extractor = new WorkItemExtractor(_workItemRegex);
                workItems.AddRange(extractor.Extract(comment));
            }
        }


        private void ReadChangeItems(ChangeSet cs, StreamReader reader)
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

        private string ReadLine(StreamReader reader)
        {
            // The only place where we read
            _lastLine = reader.ReadLine()?.Trim();
            return _lastLine;
        }

        private KindOfChange ToKindOfChange(string kind)
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
    }
}