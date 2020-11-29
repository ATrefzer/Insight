using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Insight.SvnProvider
{
    /// <summary>
    ///     Provider for Svn changeset history.
    ///     Since Svn is only a source control system work items are not provided!
    ///     For exampe we don't know if a commit was a bug fix or not.
    /// </summary>
    public sealed class SvnProvider : ISourceControlProvider
    {
        private string _cachePath;
        private string _contributionFile;


        private MappingInfo _mappingInfo;

        private string _startDirectory;

        private SvnCommandLine _svnCli;
        private string _historyFile;
        private MovementTracker _tracking;
        private string _workItemRegex;
        private string _logFile;

        /// <summary>
        ///     Returns the paths relative from the working directory
        /// </summary>
        /// <returns></returns>
        public HashSet<string> GetAllTrackedFiles()
        {
            var serverPaths = _svnCli.GetAllTrackedFiles();
            var all = serverPaths.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(all);
        }

        /// <summary>
        ///     Note: Use local path to avoid the problem that power tools are called from a non mapped directory.
        ///     Developer -> lines of code
        /// </summary>
        public Dictionary<string, uint> CalculateDeveloperWork(string localFile)
        {
            var blame = Blame(localFile);

            // Parse annotated file
            var workByDevelopers = new Dictionary<string, uint>();
            var changeSetRegex = new Regex(@"^\s*(?<revision>\d+)\s*(?<developerName>\S+)(?<codeLine>.*)",
                RegexOptions.Compiled | RegexOptions.Multiline);

            // Work by change sets (line by line)
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
        ///     Returns path to the cached file
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

                var revision = entry.Attributes["revision"].Value;
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
            _logFile = Path.Combine(cachePath, @"svn_log.txt");
            _historyFile = Path.Combine(cachePath, @"svn_history.json");
            _contributionFile = Path.Combine(cachePath, @"contribution.json");
            _svnCli = new SvnCommandLine(_startDirectory);
        }

       /// <summary>
        /// You need to call UpdateCache before.
        /// </summary>
        public ChangeSetHistory QueryChangeSetHistory()
        {
            VerifyHistoryIsCached();
            var json = File.ReadAllText(_historyFile, Encoding.UTF8);
            return JsonConvert.DeserializeObject<ChangeSetHistory>(json);
        }
        void VerifyHistoryIsCached()
        {
            if (!File.Exists(_historyFile))
            {
                var msg = $"Log export file '{_historyFile}' not found. You have to 'Sync' first.";
                throw new FileNotFoundException(msg);
            }
        }


        private void SaveHistory(ChangeSetHistory history)
        {
            var json = JsonConvert.SerializeObject(history, Formatting.Indented);
            File.WriteAllText(_historyFile, json, Encoding.UTF8);
        }

        public List<WarningMessage> Warnings { get; private set; }

        public void UpdateCache(IProgress progress, bool includeWorkData)
        {
            // Including work
            DeleteAllCaches();

            // Important: svn log returns the revisions in a different order 
            // than the {revision:HEAD} version.

            // Create directories
            GetBlameCache();
            GetHistoryCache();

            ExportLogToDisk();
            var history = ReadExportFile();
            SaveHistory(history);

            if (includeWorkData)
            {
                UpdateContribution(progress);
            }
        }

        public Dictionary<string, Contribution> QueryContribution()
        {
            // The contributions are optional
            if (!File.Exists(_contributionFile))
            {
                return null;
            }

            var input = File.ReadAllText(_contributionFile, Encoding.UTF8);
            return JsonConvert.DeserializeObject<Dictionary<string, Contribution>>(input);
        }

        public static string GetClass()
        {
            var type = typeof(SvnProvider);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }


        private List<string> GetAllTrackedLocalFiles()
        {
            var localFiles = GetAllTrackedFiles()
                .Select(MapToLocalFile_ServerIsRelativeToBaseDirectory)
                .Where(File.Exists)
                .ToList();
            return localFiles;
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

            if (File.Exists(_logFile))
            {
                File.Delete(_logFile);
            }
        }

        private void UpdateContribution(IProgress progress)
        {
            var localFiles = GetAllTrackedLocalFiles();
            var contribution = CalculateContributionsParallel(progress, localFiles.ToList());
            var json = JsonConvert.SerializeObject(contribution);
            File.WriteAllText(_contributionFile, json, Encoding.UTF8);
        }


        /// <summary>
        ///     Duplicate with git provider
        /// </summary>
        private Dictionary<string, Contribution> CalculateContributionsParallel(IProgress progress,
            List<string> localFiles)
        {
            // Calculate main developer for each file
            var fileToContribution = new ConcurrentDictionary<string, Contribution>();

            var all = localFiles.Count;
            Parallel.ForEach(localFiles, new ParallelOptions {MaxDegreeOfParallelism = 4},
                file =>
                {
                    var work = CalculateDeveloperWork(file);
                    var contribution = new Contribution(work);

                    var result = fileToContribution.TryAdd(file, contribution);
                    Debug.Assert(result);

                    // Progress
                    var count = fileToContribution.Count;

                    progress.Message($"Calculating work {count}/{all}");
                });

            return fileToContribution.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
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
            File.WriteAllText(_logFile, log);
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

        private string GetPathToExportedFile(FileInfo localFile, string revision)
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
        ///     How to map a given server path to local file system
        /// </summary>
        /// <param name="startDirectory"></param>
        /// <returns></returns>
        private MappingInfo GetMappingInfo(string startDirectory)
        {
            var mappingInfo = new MappingInfo();

            // Svn info for the base directory. This is where we want to start the analysis.
            var xml = _svnCli.Info(startDirectory);
            var dom = new XmlDocument();
            dom.LoadXml(xml);
            var url = dom.SelectSingleNode("//wcroot-abspath");
            var value = url.InnerText.Trim('^', ' ').Replace("/", "\\").TrimEnd('/', '\\');

            // Checkout folder in local file system. It may be above the base directory.
            var wcRoot = Uri.UnescapeDataString(value);
            mappingInfo.AbsolutePathToCheckoutDirectory = wcRoot;
         
            // Svn info for the svn checkout folder
            xml = _svnCli.Info(wcRoot);
            dom = new XmlDocument();
            dom.LoadXml(xml);
            url = dom.SelectSingleNode("//relative-url");
            value = url.InnerText.Trim('^', ' ').Replace("/", "\\").TrimEnd('/', '\\');
            mappingInfo.RelativeUrlToStartDirectory = Uri.UnescapeDataString(value);

            url = dom.SelectSingleNode("//relative-url");
            value = url.InnerText.Trim('^', ' ').Replace("/", "\\").TrimEnd('/', '\\');
            mappingInfo.RelativeUrlToCheckoutDirectory = Uri.UnescapeDataString(value);

            return mappingInfo;
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

        /// <summary>
        /// We obtain the server path starting with /. This corresponds to the checkout directory
        /// So we remove the relative url of the checkout directory from the server path and replace it with the svn checkout directory path
        /// </summary>
        private string MapToLocalFile_ServerIsAbsolute(string serverPath)
        {
            if (_mappingInfo == null)
            {
                _mappingInfo = GetMappingInfo(_startDirectory);
            }

            var serverNormalized = serverPath.Replace("/", "\\").TrimEnd('\\');

            var common = serverNormalized.Substring(_mappingInfo.RelativeUrlToCheckoutDirectory.Length).Trim('\\');

            // Note: second part must not start with \\
            var localPath = Path.Combine(_mappingInfo.AbsolutePathToCheckoutDirectory, common);
            return localPath;
        }

        private string MapToLocalFile_ServerIsRelativeToBaseDirectory(string serverPath)
        {
            // Simplified version when requesting the path. Server is relative to the starting directory!

            var serverNormalized = serverPath.Replace("/", "\\");
            var localPath = Path.Combine(_startDirectory, serverNormalized);
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
                    item.LocalPath = MapToLocalFile_ServerIsAbsolute(path);

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

            using (var reader = XmlReader.Create(_logFile))
            {
                while (reader.Read())
                    if (reader.IsStartElement("logentry"))
                    {
                        ParseLogEntry(reader, result);
                    }
            }

            // First item is latest commit because we got the by ordering
            Debug.Assert(result.First().Date >= result.Last().Date);

            Warnings = _tracking.Warnings;
            var history = new ChangeSetHistory(result);
            history.CleanupHistory();
            return history;
        }

        private string ReadRevision(XmlReader reader)
        {
            var revision = reader.GetAttribute("revision");
            if (revision == null)
            {
                throw new InvalidDataException();
            }

            return revision;
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
                return KindOfChange.Rename;
            }

            Debug.Assert(false);
            return KindOfChange.None;
        }

        private void UpdateWorkingCopy()
        {
            if (_svnCli.HasModifications())
            {
                throw new Exception("Abort. The repository has modifications.");
            }

            _svnCli.UpdateWorkingCopy();
        }

        /// <summary>
        ///     Returns the relative server path of the current working directory.
        ///     In svn we can choose any sub directory. "svn info" will return the
        ///     server path that corresponds to this directory.
        /// </summary>
        //private string GetServerPathForBaseDirectory(string )
        //{
        //    var xml = _svnCli.Info();
        //    var dom = new XmlDocument();
        //    dom.LoadXml(xml);
        //    var url = dom.SelectSingleNode("//relative-url");
        //    var value = url.InnerText;
        //    var path = value.Trim('^', ' ');

        //    path = Uri.UnescapeDataString(path);
        //    return path;
        //}
        private class MappingInfo
        {
            public string RelativeUrlToCheckoutDirectory;
            public string RelativeUrlToStartDirectory;
            public string AbsolutePathToCheckoutDirectory;
        }
    }
}