using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Insight.Shared;

namespace Insight.GitProvider
{
    /// <summary>
    /// Provides higher level functions and queries on a git repository.
    /// </summary>
    public sealed class GitProviderNoRenames : GitProviderBase, ISourceControlProvider
    {
        public static string GetClass()
        {
            var type = typeof(GitProviderNoRenames);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        public override void Initialize(string projectBase, string cachePath, string workItemRegex)
        {
            base.Initialize(projectBase, cachePath, workItemRegex);
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
            var log = _gitCli.LogWithoutRenames();

            var parser = new Parser(_mapper);
            parser.WorkItemRegex = _workItemRegex;
            var (history, graph) = parser.ParseLogString(log);

            // Extract master branch by tracking backwards
            var headHash = GetMasterHead();

            var allTrackedFiles = GetAllTrackedFiles(headHash);
            var trackedFileToId = allTrackedFiles.ToDictionary(serverPath => serverPath, serverPath => Guid.NewGuid().ToString());
            var aliveIds = trackedFileToId.Values.ToHashSet();

            // History is ordered descending by data. Track files while modified.
            foreach (var changeSet in history.ChangeSets)
            {
                foreach (var item in changeSet.Items)
                {
                    if (trackedFileToId.TryGetValue(item.ServerPath, out var id))
                    {
                        if (item.IsEdit())
                        {
                            item.Id = id;
                        }
                        else
                        {
                            trackedFileToId.Remove(item.ServerPath);
                        }
                    }
                }
            }

            // Remove all files from history without an id set.
            history.CleanupHistory(aliveIds);

            SaveHistory(history);
        }
    }
}