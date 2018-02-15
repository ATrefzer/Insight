using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Insight.Shared.Model
{
    [Serializable]
    public sealed class ChangeSetHistory
    {
        private readonly int WORKITEM_LIMIT = 3;

        public ChangeSetHistory(List<ChangeSet> changeSets)
        {
            ChangeSets = changeSets;
        }

        public List<ChangeSet> ChangeSets { get; private set; }

        /// <summary>
        ///     Returns a flat summary of all artifacts found in the commit history.
        /// </summary>
        public List<Artifact> GetArtifactSummary(IFilter filter, HashSet<string> localFiles)
        {
            // Item id -> artifact
            var artifacts = new Dictionary<Id, Artifact>();

            var set = new HashSet<int>();

            // Files we already know we skip are not checked again!
            var ignore = new HashSet<Id>();

            foreach (var changeset in ChangeSets)
            {
                if (changeset.WorkItems.Count >= WORKITEM_LIMIT)
                {
                    // Ignore monster merges
                    continue;
                }

                Debug.Assert(set.Add(changeset.Id)); // Change set appears only once
                foreach (var item in changeset.Items)
                {
                    // The first time we see a file (id) it is the latest version of the file.
                    // Either add it to the summary or ignore list.
                    var id = item.Id;
                    if (ignore.Contains(id))
                    {
                        // Files we already know to be skipped are not checked again! (Performance)
                        continue;
                    }

                    if (filter != null && !filter.IsAccepted(item.LocalPath))
                    {
                        ignore.Add(id);
                        continue;
                    }

                    if (!artifacts.ContainsKey(id))
                    {
                        // The changeset where we see the item the first time is the latest revision!

                        if (!Exists(item, localFiles))
                        {
                            // The history tracks files that may not exist any more.
                            // We are only interested in the current state.
                            // These files should not appear in the summary.
                            ignore.Add(id);
                            continue;
                        }

                        artifacts[id] = CreateArtifact(changeset.Id, item);
                    }

                    var artifact = artifacts[id];

                    // Aggregate information from earlier commits (for example number of commits etc)
                    // TODO ApplyTeams(teamClassifier, artifact, changeset);
                    ApplyCommits(artifact);
                    ApplyCommitters(artifact, changeset);
                    ApplyWorkItems(artifact, changeset);
                }
            }

            // Remove entries that exist on hard disk but are removed form TFS!
            // Flatten the structure and return only the artifacts.
            return artifacts.Where(pair => !pair.Value.IsDeleted).Select(pair => pair.Value).ToList();
        }

        public void Merge(ChangeSetHistory history)
        {
            var ids = ChangeSets.ToLookup(cs => cs.Id);

            foreach (var cs in history.ChangeSets)
            {
                if (!ids.Contains(cs.Id))
                {
                    ChangeSets.Add(cs);
                }
            }

            // TODO No, No, No Does not work with git!
            // But is used for svn only!
            ChangeSets = ChangeSets.OrderByDescending(cs => cs.Id).ToList();
        }

        private static void ApplyCommits(Artifact artifact)
        {
            artifact.Commits = artifact.Commits + 1;
        }

        private static void ApplyCommitters(Artifact artifact, ChangeSet changeset)
        {
            artifact.Committers.Add(changeset.Committer);
        }

        private static void ApplyTeams(ITeamClassifier teamClassifier, Artifact artifact, ChangeSet changeset)
        {
            if (teamClassifier != null)
            {
                // If a classifier is given use it ...
                artifact.Teams.Add(teamClassifier.GetAssociatedTeam(changeset.Committer, changeset.Date));
            }
        }

        private static void ApplyWorkItems(Artifact artifact, ChangeSet changeset)
        {
            foreach (var workitem in changeset.WorkItems)
            {
                artifact.WorkItems.Add(workitem);
            }
        }

        private static Artifact CreateArtifact(int changeSetId, ChangeItem item)
        {
            Debug.Assert(item.LocalPath != null);
            var artifact = new Artifact
                           {
                                   Id = item.Id,
                                   LocalPath = item.LocalPath,
                                   ServerPath = item.ServerPath,
                                   Commits = 0,

                                   // Item used to create sets the revision (latest)
                                   Revision = changeSetId,

                                   // Assume first item is latest revision. If this is deleted the item should no longer be on hard disk.
                                   IsDeleted = item.IsDelete()
                           };

            return artifact;
        }

        private bool Exists(ChangeItem item, HashSet<string> localFiles)
        {
            if (localFiles != null && localFiles.Any())
            {
                // Performance optimization
                return localFiles.Contains(item.LocalPath.ToLowerInvariant());
            }

            return item.Exists();
        }
    }
}