using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;

namespace Insight.GitProvider
{
    public abstract class GitProviderBase
    {
        protected string _cachePath;
        protected IFilter _fileFilter;
        protected GitCommandLine _gitCli;
        protected string _gitHistoryExportFile;

        protected string _lastLine;
        protected PathMapper _mapper;
        protected string _startDirectory;
        protected string _workItemRegex;

        public List<WarningMessage> Warnings { get; protected set; }

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
                developer = Decoder.DecodeUtf8(developer).Trim('\t');
                workByDevelopers.AddToValue(developer, 1);
            }

            return workByDevelopers;
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

        protected ChangeSetHistory ParseLogString(string gitLogString)
        {
            var parser = new Parser(_mapper, null);
            parser.WorkItemRegex = _workItemRegex;
            return parser.ParseLogString(gitLogString);
        }

        protected void VerifyGitDirectory()
        {
            if (!Directory.Exists(Path.Combine(_startDirectory, ".git")))
            {
                // We need the root (containing .git) because of the function MapToLocalFile.
                // We could go upwards and find the git root ourself and use this root for the path mapping.
                // But for the moment take everything. The user can set filters in the project settings.
                throw new ArgumentException("The given start directory is not the root of a git repository.");
            }
        }

        protected void VerifyHistoryIsCached()
        {
            if (!File.Exists(_gitHistoryExportFile))
            {
                var msg = $"Log export file '{_gitHistoryExportFile}' not found. You have to 'Sync' first.";
                throw new FileNotFoundException(msg);
            }
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
    }
}