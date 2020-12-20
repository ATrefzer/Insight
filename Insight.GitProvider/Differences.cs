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

            var intersect = DiffToParent1.Intersect(DiffToParent2).ToList();
            ChangesInCommit = intersect;

            DiffExclusiveToParent1 = DiffToParent1.Except(intersect).ToList();
            DiffExclusiveToParent2 = DiffToParent2.Except(intersect).ToList();
        }
    }
}