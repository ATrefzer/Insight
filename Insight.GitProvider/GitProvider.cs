using Insight.Shared;
using Insight.Shared.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insight.GitProvider
{
    public class GitProvider : ISourceControlProvider
    {
        private string _startDirectory;
        private string _cachePath;
        private string _workItemRegex;
        private object _gitHistoryExportFile;
        private object _historyBinCacheFile;
        private GitCommandLine _gitCli;

        public void Initialize(string projectBase, string cachePath, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;

            // TODO really needd?
            _gitHistoryExportFile = Path.Combine(cachePath, @"git_history.log");
            _historyBinCacheFile = Path.Combine(cachePath, @"cs_history.bin");
            _gitCli = new GitCommandLine(_startDirectory);

        }
        public static string GetClass()
        {
            var type = typeof(GitProvider);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        public Dictionary<string, int> CalculateDeveloperWork(Artifact artifact)
        {
            throw new NotImplementedException();
        }

        public List<FileRevision> ExportFileHistory(string localFile)
        {
            throw new NotImplementedException();
        }

        public ChangeSetHistory QueryChangeSetHistory()
        {
            throw new NotImplementedException();
        }

        public ChangeSetHistory UpdateCache()
        {
            // Git has the complete history locally anyway.
            // So we just can fetch and pull any changes.

            AbortOnPotentialMergeConflicts();

            _gitCli.PullMasterFromOrigin();


            throw new Exception("Not implemented - no local changes");
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
    }
}
