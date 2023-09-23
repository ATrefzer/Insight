using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;

using Newtonsoft.Json;

namespace Insight.GitProvider
{
    public abstract class GitProviderBase
    {
        protected string _cachePath;
        protected GitCommandLine _gitCli;
        protected string _historyFile;
        protected string _contributionFile;

        protected PathMapper _mapper;
        protected string _projectBase;
        protected string _workItemRegex;

        public List<WarningMessage> Warnings { get; protected set; }

        /// <summary>
        /// The start directory.
        /// </summary>
        public string BaseDirectory => _projectBase;

        /// <summary>
        /// <inheritdoc cref="ISourceControlProvider.CalculateDeveloperWork"/>
        /// </summary>
        public Dictionary<string, uint> CalculateDeveloperWork(string localFile)
        {
            var annotate = _gitCli.Annotate(localFile);

            //S = not a whitespace
            //s = whitespace

            // Parse annotated file
            var workByDevelopers = new Dictionary<string, uint>();
            var changeSetRegex = new Regex(@"^\S+\t\(\s*(?<developerName>[^\t]+).*", RegexOptions.Multiline | RegexOptions.Compiled);

            // Work by change sets (line by line)
            var matches = changeSetRegex.Matches(annotate);
            foreach (Match match in matches)
            {
                var developer = match.Groups["developerName"].Value;
                developer = developer.Trim('\t');
                workByDevelopers.AddToValue(developer, 1);
            }

            return workByDevelopers;
        }

        /// <summary>
        /// <inheritdoc cref="ISourceControlProvider.ExportFileHistory"/>
        /// </summary>
        public List<FileRevision> ExportFileHistory(string localFile)
        {
            var result = new List<FileRevision>();

            var log = _gitCli.Log(localFile);

            var historyOfSingleFile = ParseLogString(log);
            foreach (var cs in historyOfSingleFile.ChangeSets)
            {
                var changeItem = cs.Items.First();

                var fi = new FileInfo(localFile);
                var exportFile = GetPathToExportedFile(fi, cs.Id);

                if (!changeItem.IsDelete())
                {
                    // Download if not already in cache
                    if (!File.Exists(exportFile))
                    {
                        // If item is deleted we won't find it in this changeset.
                        _gitCli.ExportFileRevision(changeItem.ServerPath, cs.Id, exportFile);
                    }

                    var revision = new FileRevision(changeItem.LocalPath, cs.Id, cs.Date, exportFile);
                    result.Add(revision);
                }
            }

            return result;
        }

        public HashSet<string> GetAllTrackedFiles()
        {
            return GetAllTrackedFiles(null);
        }


        public HashSet<string> GetAllTrackedFiles(string hash)
        {
            var serverPaths = _gitCli.GetAllTrackedFiles(hash);
            var all = serverPaths.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(all.Select(Decoder.DecodeEscapedBytes));
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

        protected List<string> GetAllTrackedLocalFiles()
        {
            var trackedServerPaths = GetAllTrackedFiles();

            // Filtered local paths
            return trackedServerPaths.Select(sp => _mapper.MapToLocalFile(sp)).ToList();
        }

        protected string GetMasterHead()
        {
            return _gitCli.GetMasterHead(_projectBase);
        }


        protected void UpdateContribution(IProgress progress, IFilter fileTypeFilter)
        {
            if (File.Exists(_contributionFile))
            {
                File.Delete(_contributionFile);
            }

            var allLocalFiles = GetAllTrackedLocalFiles();

            var localFiles = allLocalFiles.Where(fileTypeFilter.IsAccepted).ToList();
            var contribution = CalculateContributionsParallel(progress, localFiles);

            var json = JsonConvert.SerializeObject(contribution);

            File.WriteAllText(_contributionFile, json, Encoding.UTF8);
        }

        protected Dictionary<string, Contribution> CalculateContributionsParallel(IProgress progress, List<string> localFiles)
        {
            // Calculate main developer for each file
            var fileToContribution = new ConcurrentDictionary<string, Contribution>();

            var all = localFiles.Count;
            Parallel.ForEach(localFiles,
                             file =>
                             {
                                 var work = CalculateDeveloperWork(file);
                                 var contribution = new Contribution(work);

                                 if (work.Any()) // get rid of 0 byte files.
                                 {
                                     var result = fileToContribution.TryAdd(file, contribution);
                                     Debug.Assert(result);
                                 }

                                 // Progress
                                 var count = fileToContribution.Count;

                                 progress.Message($"Calculating work {count}/{all}");
                             });

            return fileToContribution.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
        }

        protected ChangeSetHistory ParseLogString(string gitLogString)
        {
            var parser = new Parser(_mapper);
            parser.WorkItemRegex = _workItemRegex;
            return parser.ParseLogStringNoGraph(gitLogString);
        }

        protected void VerifyGitPreConditions()
        {
            if (!Directory.Exists(Path.Combine(_projectBase, ".git")))
            {
                // We need the root (containing .git) because of the function MapToLocalFile.
                throw new ArgumentException("The given start directory is not the root of a git repository.");
            }

            if (!_gitCli.IsMasterGetCheckedOut())
            {
                // Branch may be developer or main etc.
                //throw new ArgumentException("The currently checked out branch is not the master branch.");
            }

            if (_gitCli.HasIndexOrWorkspaceChanges())
            {
                throw new ArgumentException("There are local changes that are not committed yet. This may give invalid results.");
            }
        }

        protected void VerifyHistoryIsCached()
        {
            if (!File.Exists(_historyFile))
            {
                var msg = $"Log export file '{_historyFile}' not found. You have to 'Sync' first.";
                throw new FileNotFoundException(msg);
            }
        }

        protected void VerifyContributionIsCached()
        {
            if (!File.Exists(_contributionFile))
            {
                var msg = $"Contribution file '{_contributionFile}' not found. You have to 'Sync' first.";
                throw new FileNotFoundException(msg);
            }
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

        protected void SaveHistory(ChangeSetHistory history)
        {
            var json = JsonConvert.SerializeObject(history, Formatting.Indented);
            File.WriteAllText(_historyFile, json, Encoding.UTF8);
        }


        public virtual void Initialize(string projectBase, string cachePath, string workItemRegex)
        {
            _projectBase = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;

            _historyFile = Path.Combine(cachePath, "git_history.json");
            _contributionFile = Path.Combine(cachePath, "contribution.json");
            _gitCli = new GitCommandLine(_projectBase);

            // "/" maps to _startDirectory
            _mapper = new PathMapper(_projectBase);
        }

        protected void SaveFullLogToDisk()
        {
            var log = _gitCli.Log();
            File.WriteAllText(Path.Combine(_cachePath, "git_full_history.txt"), log);
        }
    }
}