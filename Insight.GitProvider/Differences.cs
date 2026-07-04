using System.Collections.Generic;
using System.Linq;

using LibGit2Sharp;

namespace Insight.GitProvider
{
    public sealed class Differences
    {
        public List<TreeEntryChanges> DiffToParent1 {get; }
        public List<TreeEntryChanges> DiffToParent2  { get; }

        public List<TreeEntryChanges> DiffExclusiveToParent1 {get;}
        public List<TreeEntryChanges> DiffExclusiveToParent2  { get; }

        /// <summary>
        /// For a merge node this contains changes done in the commit itself.
        /// </summary>
        public List<TreeEntryChanges> ChangesInCommit  { get; }


        public Differences(List<TreeEntryChanges> diffToParent)
        {
            DiffToParent1 = diffToParent ?? new List<TreeEntryChanges>();
            DiffToParent2 = null;
            ChangesInCommit = diffToParent;
            DiffExclusiveToParent1 = diffToParent;
            DiffExclusiveToParent2 = null;
        }

        public Differences(List<TreeEntryChanges> deltaToParent1, List<TreeEntryChanges> deltaToParent2)
        {
            DiffToParent1 = deltaToParent1 ?? new List<TreeEntryChanges>();
            DiffToParent2 = deltaToParent2 ?? new List<TreeEntryChanges>();

            // A file that differs from both parents was touched in the merge commit itself
            // (conflict resolution or "evil merge"). We have to compare by path.
            // TreeEntryChanges has no value equality, and the old oids differ per parent anyway.
            var pathsToParent2 = new HashSet<string>(DiffToParent2.Select(change => change.Path));
            ChangesInCommit = DiffToParent1.Where(change => pathsToParent2.Contains(change.Path)).ToList();

            var pathsInBoth = new HashSet<string>(ChangesInCommit.Select(change => change.Path));
            DiffExclusiveToParent1 = DiffToParent1.Where(change => !pathsInBoth.Contains(change.Path)).ToList();
            DiffExclusiveToParent2 = DiffToParent2.Where(change => !pathsInBoth.Contains(change.Path)).ToList();
        }
    }
}