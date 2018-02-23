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
using Insight.Shared.VersionControl;

namespace Insight.SvnProvider
{
    /// <summary>
    /// Provider for Svn changeset history.
    /// Since Svn is only a source control system work items are not provided!
    /// For exampe we don't know if a commit was a bug fix or not.
    /// </summary>
    public sealed class SvnProvider : ISourceControlProvider
    {
        private string _cachePath;

        private string _serverRoot;
        private string _startDirectory;

        private SvnCommandLine _svnCli;

        private string _svnHistoryExportFile;
        private MovementTracker _tracking;
        private string _workItemRegex;


        public static string GetClass()
        {
            var type = typeof(SvnProvider);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        // TODO returns path based on the working directory.
        public HashSet<string> GetAllTrackedFiles()
        {
            var serverPaths = _svnCli.GetAllTrackedFiles();
            var all = serverPaths.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(all);
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

                var value = ulong.Parse(entry.Attributes["revision"].Value);
                var revision =  new NumberId(value);
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

        public void Initialize(string projectBase, string cachePath, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;
            _svnHistoryExportFile = Path.Combine(cachePath, @"svn_history.log");
            _svnCli = new SvnCommandLine(_startDirectory);
        }

        /// <summary>
        /// You need to call UpdateCache before.
        /// </summary>
        public ChangeSetHistory QueryChangeSetHistory()
        {
            if (!File.Exists(_svnHistoryExportFile))
            {
                var msg = $"Log export file '{_svnHistoryExportFile}' not found. You have to 'Sync' first.";
                throw new FileNotFoundException(msg);
            }

            return ReadExportFile();
        }

        public List<WarningMessage> Warnings { get; private set; }

        public void UpdateCache()
        {
            // Important: svn log returns the revisions in a different order 
            // than the {revision:HEAD} version.

            // Create directories
            GetBlameCache();
            GetHistoryCache();
            _serverRoot = null;

            ExportLogToDisk();
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


        private void ExportLogToDisk()
        {
            // Important: The svn log command only returns the history up to the revision
            // that is on the local working copy. So it is important to update first!
            // UpdateWorkingCopy(); -> No, it is not. Let the user do this.

            var log = _svnCli.Log();

            // Override existing file
            File.WriteAllText(_svnHistoryExportFile, log);
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

        /// <summary>
        /// Returns the relative server path of the current working directory.
        /// In svn we can choose any sub directory. "svn info" will return the
        /// server path that corresponds to this directory.
        /// </summary>
        private string GetServerPathForBaseDirectory()
        {
            var xml = _svnCli.Info();
            var dom = new XmlDocument();
            dom.LoadXml(xml);
            var url = dom.SelectSingleNode("//relative-url");
            var value = url.InnerText;
            var path = value.Trim('^', ' ');

            path = Uri.UnescapeDataString(path);
            return path;
        }

        private string GetStringAttribute(XmlReader reader, string name)
        {
            var value = reader.GetAttribute(name);
            return value;
        }

        private ulong GetULongAttribute(XmlReader reader, string name)
        {
            var value = reader.GetAttribute(name);
            if (value == null)
            {
                throw new InvalidDataException(name);
            }

            return ulong.Parse(value);
        }

        private string MapToLocalFile(string serverPath)
        {
            // In svn we can select any sub directory of our working copy.
            // GetServerPathForBaseDirectory returns an updated relative path!
            var serverNormalized = serverPath.Replace("/", "\\");
            if (_serverRoot == null)
            {
                _serverRoot = GetServerPathForBaseDirectory();
            }

            var common = serverNormalized.Substring(_serverRoot.Length).Trim('\\');
            var localPath = Path.Combine(_startDirectory, common);
            return localPath;
        }

        private void ParseLogEntry(XmlReader reader, List<ChangeSet> result)
        {
            var cs = new ChangeSet();
           

            // revision -> Id 
            var revision = ReadRevision(reader);
            cs.Id = revision;
            _tracking.BeginChangeSet(cs);

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
                        var copyFromPath = GetStringAttribute(reader, "copyfrom-path");
                        if (copyFromPath != null)
                        {
                            var id = GetULongAttribute(reader, "copyfrom-rev");
                            var copyFromRev = new NumberId(id);
                            item.FromServerPath = copyFromPath;
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

                    if (item.Kind == KindOfChange.Rename && item.FromServerPath == null)
                    {
                        // Wtf. This can happen. Just ignore it.
                    }
                    else
                    {
                        _tracking.TrackId(item);                     
                    }
                
                } while (reader.ReadToNextSibling("path"));

                if (!reader.ReadToFollowing("msg"))
                {
                    throw new InvalidDataException("msg");
                }

                cs.Comment = reader.ReadString().Trim();
                ParseWorkItemsFromComment(cs.WorkItems, cs.Comment);
            }

            // Applies all change set items and sets their id
           _tracking.ApplyChangeSet(cs.Items);

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
            _tracking = new MovementTracker();
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

            // First item is latest commit because we got the by ordering
            Debug.Assert(result.First().Date >= result.Last().Date);

            Warnings = _tracking.Warnings;
            return new ChangeSetHistory(result);
        }

        private NumberId ReadRevision(XmlReader reader)
        {
            var revision = reader.GetAttribute("revision");
            if (revision == null)
            {
                throw new InvalidDataException();
            }

            var value = ulong.Parse(revision);
            return new NumberId(value);
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

        private void UpdateWorkingCopy()
        {
            if (_svnCli.HasModifications())
            {
                // I don't want to run into merge conflicts.
                throw new Exception("Abort. The repository has modifications.");
            }

            _svnCli.UpdateWorkingCopy();
        }
    }
}