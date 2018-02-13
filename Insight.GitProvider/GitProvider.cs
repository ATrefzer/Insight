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
            // Take care that no local modifications are present.

            // TODO check for local modifications. Also in Svn
            return new ChangeSetHistory(null);
        }
    }
}
