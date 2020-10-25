using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;

using Insight.Shared;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;

using Newtonsoft.Json;

namespace Insight.GitProvider
{
    /// <summary>
    /// Provides higher level functions and queries on a git repository.
    /// </summary>
    public sealed class GitProviderMasterOnly : GitProviderBase, ISourceControlProvider
    {
        public static string GetClass()
        {
            var type = typeof(GitProviderMasterOnly);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        public void Initialize(string projectBase, string cachePath, IFilter fileFilter, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;
            _fileFilter = fileFilter;

            _historyFile = Path.Combine(cachePath, "git_history.json");
            _contributionFile = Path.Combine(cachePath, "contribution.json");
            _gitCli = new GitCommandLine(_startDirectory);
            _mapper = new PathMapper(_startDirectory);
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
        }

        public void UpdateCache(IProgress progress, bool includeWorkData)
        {
            DeleteAllCaches();

            VerifyGitPreConditions();

            UpdateHistory();

            if (includeWorkData)
            {
                // Optional
                UpdateContribution(progress);
            }
        }

    
        

        private void UpdateHistory()
        {
            var log = _gitCli.Log();

            var parser = new Parser(_mapper);
            parser.WorkItemRegex = _workItemRegex;
            var (history, graph) = parser.ParseLogString(log);

            // Extract master branch by tracking backwards
            var headHash = GetMasterHead();


            var masterNodes = new List<GraphNode>();
            var headNode = graph.GetNode(headHash);
            while (headNode != null)
            {
                masterNodes.Add(headNode);

                // The first parent is the branch that was checked out when we merged.
                headNode = headNode.Parents.FirstOrDefault();
              
            }

            var masterHashes = masterNodes.Select(node => node.CommitHash).ToHashSet();
            var masterChangeSets = history.ChangeSets.Where(cs => masterHashes.Contains(cs.Id));
            var masterHistory = new ChangeSetHistory(masterChangeSets.OrderByDescending(x => x.Date).ToList());
            


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
            var json = JsonConvert.SerializeObject(masterHistory, Formatting.Indented);
            File.WriteAllText(_historyFile, json, Encoding.UTF8);

            // For information
            File.WriteAllText(Path.Combine(_cachePath, @"git_master_history.txt"), log);
        }
    }
}