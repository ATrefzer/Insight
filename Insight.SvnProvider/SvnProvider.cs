using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;

namespace Insight.SvnProvider
{
    /// <summary>
    /// Provider for Svn changeset history.
    /// Since Svn is only a source control system work items are not provided!
    /// For exampe we don't know if a commit was a bug fix or not.
    /// </summary>
    public sealed class SvnProvider : ISourceControlProvider
    {
        private readonly string _cachePath;
        private readonly string _historyBinCacheFile;
        private readonly string _startDirectory;

        private readonly SvnCommandLine _svnCli;

        private readonly string _svnHistoryExportFile;
        private readonly MovementTracking _tracking = new MovementTracking();
        private readonly string _workItemRegex;

        /// <summary>
        /// For mapping the server path to local path
        /// </summary>
        private string _prefix;

        public SvnProvider(string projectBase, string cachePath, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;
            _svnHistoryExportFile = Path.Combine(cachePath, @"svn_history.log");
            _historyBinCacheFile = Path.Combine(cachePath, @"cs_history.bin");
            _svnCli = new SvnCommandLine(_startDirectory);
        }

        public static string GetClass()
        {
            var type = typeof(SvnProvider);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        /// <summary>
        /// Note: Use local path to avoid the problem that power tools are called from a non mapped directory.
        /// Developer -> lines of code
        /// </summary>
        public Dictionary<string, int> CalculateDeveloperWork(Artifact artifact)
        {
            var blame = Blame(artifact.LocalPath);

            // Parse annotated file
            var workByDevelopers = new Dictionary<string, int>();
            var changeSetRegex = new Regex(@"^\s*(?<revision>\d+)\s*(?<developerName>\S+)(?<codeLine>.*)",
                                           RegexOptions.Compiled | RegexOptions.Multiline);

            // Work by changesets (line by line)
            var matches = changeSetRegex.Matches(blame);
            foreach (Match match in matches)
            {
                var developer = match.Groups["developerName"].Value;
                var revision = int.Parse(match.Groups["revision"].Value, CultureInfo.InvariantCulture);
                workByDevelopers.AddToValue(developer, 1);
            }

            return workByDevelopers;
        }

        /// <summary>
        /// Returns path to the cached file
        /// </summary>
        public List<FileRevision> ExportFileHistory(string localFile)
        {
            var result = new List<FileRevision>();

            var xml = _svnCli.GetRevisionsForLocalFile(localFile);

            var dom = new XmlDocument();
            dom.LoadXml(xml);
            var entries = dom.SelectNodes("//logentry");

            if (entries == null)
            {
                return result;
            }

            foreach (XmlNode entry in entries)
            {
                if (entry?.Attributes == null)
                {
                    continue;
                }

                var revision = int.Parse(entry.Attributes["revision"].Value);
                var date = entry.SelectSingleNode("./date")?.InnerText;
                var dateTime = DateTime.Parse(date);

                // Get historical version from file cache

                var fi = new FileInfo(localFile);
                var exportFile = GetPathToExportedFile(fi, revision);

                // Download if not already in cache
                if (!File.Exists(exportFile))
                {
                    _svnCli.ExportFileRevision(localFile, revision, exportFile);
                }

                result.Add(new FileRevision(localFile, revision, dateTime, exportFile));
            }

            return result;
        }

        /// <summary>
        /// You need to call UpdateCache before.
        /// </summary>
        public ChangeSetHistory QueryChangeSetHistory()
        {
            if (!File.Exists(_historyBinCacheFile))
            {
                var msg = $"History cache file '{_historyBinCacheFile}' not found. You have to 'Sync' first.";
                throw new FileNotFoundException(msg);
            }

            var binFile = new BinaryFile<ChangeSetHistory>();
            return binFile.Read(_historyBinCacheFile);
        }

        public ChangeSetHistory UpdateCache()
        {
            // Important: svn log returns the revisions in a different order 
            // than the {revision:HEAD} version.

            // Create directories
            GetBlameCache();
            GetHistoryCache();

            // Incremential update of cached change set history.
            var latestRevision = -1;
            ChangeSetHistory history = null;

            // Read cached history from binary file
            if (File.Exists(_historyBinCacheFile))
            {
                var binFile = new BinaryFile<ChangeSetHistory>();
                history = binFile.Read(_historyBinCacheFile);
            }

            // Get latest know revision
            if (history != null)
            {
                latestRevision = GetLastKnownRevision(history);
            }

            if (history == null || latestRevision == -1)
            {
                history = ReadFullHistory();
            }
            else
            {
                ReadHistoryIncrement(history, latestRevision);
            }

            var outFile = new BinaryFile<ChangeSetHistory>();
            outFile.Write(_historyBinCacheFile, history);
            return history;
        }

        private static int GetLastKnownRevision(ChangeSetHistory history)
        {
            var latestRevision = -1;
            var cs = history.ChangeSets.FirstOrDefault();
            if (cs != null)
            {
                // Last known revision
                latestRevision = cs.Id;
            }

            // Plausibility check
            var maxId = history.ChangeSets.Select(csi => csi.Id).DefaultIfEmpty(-1).Max();
            Debug.Assert(maxId == latestRevision);
            return latestRevision;
        }

        private string Blame(string path)
        {
            var fileName = new FileInfo(path).Name + path.GetHashCode().ToString("X");
            var cachedPath = Path.Combine(GetBlameCache(), fileName);
            if (File.Exists(cachedPath))
            {
                return File.ReadAllText(cachedPath);
            }

            var blame = _svnCli.BlameFile(path);

            File.WriteAllText(cachedPath, blame);
            return blame;
        }

        private string GetBlameCache()
        {
            var path = Path.Combine(_cachePath, "Blame");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
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

        private int GetIntAttribute(XmlReader reader, string name)
        {
            var value = reader.GetAttribute(name);
            if (value == null)
            {
                throw new InvalidDataException(name);
            }

            return int.Parse(value);
        }

        private string GetPathToExportedFile(FileInfo localFile, int revision)
        {
            var name = new StringBuilder();

           
            name.Append(localFile.FullName.GetHashCode().ToString("X"));
            name.Append("_");
            name.Append(revision.ToString("X"));
            name.Append("_");
            name.Append(localFile.Name);

            return Path.Combine(GetHistoryCache(), name.ToString());
        }

        private string GetStringAttribute(XmlReader reader, string name)
        {
            var value = reader.GetAttribute(name);
            return value;
        }

        private string MapToLocalFile(string serverPath)
        {
            var serverNormalized = serverPath.Replace("/", "\\");
            if (_prefix == null)
            {
                // Cut away as much from the base directory such that it fits the beginning of the server path
                var index = 0;
                while (index <= _startDirectory.Length)
                {
                    var baseRemainder = _startDirectory.Substring(index);
                    if (serverNormalized.StartsWith(baseRemainder, StringComparison.InvariantCulture))
                    {
                        // found 
                        _prefix = _startDirectory.Substring(0, index);
                        break;
                    }

                    index++;
                }
            }

            Debug.Assert(_prefix != null);
            return _prefix + serverNormalized;
        }

        private void ParseLogEntry(XmlReader reader, List<ChangeSet> result)
        {
            var cs = new ChangeSet();

            // revision -> Id 
            var revision = ReadRevision(reader);
            cs.Id = revision;

            // author -> Committer
            if (!reader.ReadToDescendant("author"))
            {
                throw new InvalidDataException("author");
            }

            cs.Committer = reader.ReadString();

            // date -> date
            if (!reader.ReadToNextSibling("date"))
            {
                throw new InvalidDataException("date");
            }

            cs.Date = DateTime.Parse(reader.ReadString().Trim());

            if (!reader.ReadToNextSibling("paths"))
            {
                throw new InvalidDataException("paths");
            }

            if (reader.ReadToDescendant("path"))
            {
                do
                {
                    string copyFromPath = null;
                    var copyFromRev = -1;

                    var item = new ChangeItem();

                    var kind = reader.GetAttribute("kind");
                    if (kind != "file")
                    {
                        continue;
                    }

                    var action = reader.GetAttribute("action");
                    item.Kind = SvnActionToKindOfChange(action);
                    if (item.IsRename() || item.IsAdd())
                    {
                        // Both actions can mean a renaming or movement.
                        copyFromPath = GetStringAttribute(reader, "copyfrom-path");
                        if (copyFromPath != null)
                        {
                            copyFromRev = GetIntAttribute(reader, "copyfrom-rev");
                        }
                    }
                    else
                    {
                        Debug.Assert(string.IsNullOrEmpty(GetStringAttribute(reader, "copyfrom-path")));
                        Debug.Assert(string.IsNullOrEmpty(GetStringAttribute(reader, "copyfrom-rev")));
                    }

                    // All attributes must have been read here.

                    var path = reader.ReadString().Trim();
                    item.ServerPath = path;
                    item.LocalPath = MapToLocalFile(path);

                    item.Id = new StringId(item.ServerPath);

                    cs.Items.Add(item);

                    // All info available
                    if (copyFromPath != null && copyFromRev != -1)
                    {
                        _tracking.Add(revision, new StringId(item.ServerPath), copyFromRev, new StringId(copyFromPath));
                    }
                } while (reader.ReadToNextSibling("path"));

                if (!reader.ReadToFollowing("msg"))
                {
                    throw new InvalidDataException("msg");
                }

                cs.Comment = reader.ReadString().Trim();
                ParseWorkItemsFromComment(cs.WorkItems, cs.Comment);
            }

            result.Add(cs);
        }

        private void ParseWorkItemsFromComment(List<WorkItem> workItems, string comment)
        {
            if (!string.IsNullOrEmpty(_workItemRegex))
            {
                var extractor = new WorkItemExtractor(_workItemRegex);
                workItems.AddRange(extractor.Extract(comment));
            }
        }

        private ChangeSetHistory ReadExportFile()
        {
            _tracking.Clear();
            var result = new List<ChangeSet>();

            using (var reader = XmlReader.Create(_svnHistoryExportFile))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement("logentry"))
                    {
                        ParseLogEntry(reader, result);
                    }
                }
            }

            var ordered = result.OrderByDescending(cs => cs.Id).ToList();
            var history = new ChangeSetHistory(ordered);
            return history;
        }

        private ChangeSetHistory ReadFullHistory()
        {
            // Important: The svn log command only returns the history up to the revision
            // that is on the local working copy. So it is important to update first!
            UpdateWorkingCopy();

            var log = _svnCli.Log();

            File.WriteAllText(_svnHistoryExportFile, log);

            var history = ReadExportFile();
            UpdateMovedFileIds(history);
            return history;
        }

        private void ReadHistoryIncrement(ChangeSetHistory history, int revision)
        {
            var log = _svnCli.Log(revision);

            File.WriteAllText(_svnHistoryExportFile, log);
            var newChangeSets = ReadExportFile();
            history.Merge(newChangeSets);

            // A few new renamings update all older existing ids.
            // Always use the latest ids for the whole renaming / moving chain.
            UpdateMovedFileIds(history);
        }

        private int ReadRevision(XmlReader reader)
        {
            var revision = reader.GetAttribute("revision");
            if (revision == null)
            {
                throw new InvalidDataException();
            }

            return int.Parse(revision);
        }

        private KindOfChange SvnActionToKindOfChange(string action)
        {
            if (action == "M")
            {
                return KindOfChange.Edit;
            }

            if (action == "A")
            {
                return KindOfChange.Add;
            }

            if (action == "D")
            {
                return KindOfChange.Delete;
            }

            if (action == "R")
            {
                // For example change the location of the item in source safe.
                return KindOfChange.Rename;
            }

            Debug.Assert(false);
            return KindOfChange.None;
        }

        private void UpdateMovedFileIds(ChangeSetHistory history)
        {
            _tracking.RemoveInvalid();
            foreach (var cs in history.ChangeSets)
            {
                foreach (var file in cs.Items)
                {
                    // Use the id of the latest item if we can track move or rename operations
                    file.Id = _tracking.GetLatestId(file.Id, cs.Id);
                }
            }
        }

        private void UpdateWorkingCopy()
        {
            _svnCli.UpdateWorkingCopy();
        }
    }
}