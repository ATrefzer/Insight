using System.Collections.Generic;

using Insight.Shared.Model;

namespace Insight.Shared
{
    public interface ISourceControlProvider
    {
        // TODO initialize! And ctro without arguments.

        Dictionary<string, int> CalculateDeveloperWork(Artifact artifact);
        ChangeSetHistory QueryChangeSetHistory();

        /// <summary>
        /// Read the history from the source control provider and store it offline in the file system.
        /// </summary>
        ChangeSetHistory UpdateCache();

        /// <summary>
        /// Returns path to the cached file
        /// </summary>
        List<FileRevision> ExportFileHistory(string localFile);
    }
}