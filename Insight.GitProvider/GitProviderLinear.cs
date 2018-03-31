using System.IO;
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
    public sealed class GitProviderLinear : GitProviderBase, ISourceControlProvider
    {
        public static string GetClass()
        {
            var type = typeof(GitProviderLinear);
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

            var log = _gitCli.Log();

            var parser = new Parser(_mapper, null);
            parser.WorkItemRegex = _workItemRegex;
            var history = parser.ParseLogString(log);

            // Update Ids for files
            var tracker = new MovementTracker();
            foreach (var cs in history.ChangeSets)
            {
                tracker.BeginChangeSet(cs);
                foreach (var item in cs.Items)
                {
                    tracker.TrackId(item);
                }

                cs.Items.Clear();
                tracker.ApplyChangeSet(cs.Items);
            }

            Warnings = tracker.Warnings;

            // Write history file
            var json = JsonConvert.SerializeObject(history, Formatting.Indented);
            File.WriteAllText(_gitHistoryExportFile, json, Encoding.UTF8);

            // For information
            File.WriteAllText(Path.Combine(_cachePath, @"git_full_history.txt"), log);
        }
    }
}