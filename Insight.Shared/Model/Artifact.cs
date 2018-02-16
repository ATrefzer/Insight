using System.Collections.Generic;

namespace Insight.Shared.Model
{
    /// <summary>
    /// An artefact is a file committed to the source control system.
    /// </summary>
    public class Artifact
    {
        public int Commits { get; set; }

        public HashSet<string> Committers { get; } = new HashSet<string>();

        /// <summary>
        /// If the source control system does not provide unique ids like in svn use the StringId with
        /// server path.
        /// </summary>
        public Id Id { get; set; }

        /// <summary>
        ///  It still may be around on hard disk but removed from TFS!
        /// </summary>
        public bool IsDeleted { get; set; }

        public string LocalPath { get; set; }

        public ulong Revision { get; set; }

        public string ServerPath { get; set; }

        public HashSet<string> Teams { get; } = new HashSet<string>();

        public HashSet<WorkItem> WorkItems { get; } = new HashSet<WorkItem>();
    }
}