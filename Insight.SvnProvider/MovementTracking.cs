using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Insight.Shared.Model;

namespace Insight.SvnProvider
{
    /// <summary>
    /// Tracks files that are renamed or moved.
    /// Files that are copied into multiple files are ignored. The new files get their own id.
    /// </summary>
    public sealed class MovementTracking
    {
        // old id -> movement information
        private Dictionary<Id, MoveInfo> _ids = new Dictionary<Id, MoveInfo>();

        public void Add(NumberId newRevision, Id newId, NumberId oldRevision, Id oldId)
        {
            Debug.Assert(newId != null);
            if (oldId.Equals(newId))
            {
                // Encountered this rare case. No idea what it is useful for.
                // Just keep the id as it is.
                return;
            }

            if (_ids.ContainsKey(oldId))
            {
                // The source file was "renamed" or "added" into multiple others. 
                // Seems to be possible with svn. These are only a few cases.
                var item = _ids[oldId];
                item.HasMoreThanOneCopies = true; // Removed later.
                return;
            }

            _ids.Add(oldId, new MoveInfo(newRevision, newId, oldRevision, oldId));
        }

        public Id GetLatestId(Id oldId, Id oldRevision)
        {
            if (!_ids.ContainsKey(oldId))
            {
                return oldId;
            }

            // Stop on cycle to new id
            var id = oldId;
            var revision = oldRevision;

            while (_ids.TryGetValue(id, out var tmp))
            {
                // There is a newer id for the given file id.

                var numberId = (NumberId)revision;
                if (tmp.NewRevision.Value < numberId.Value)
                {
                    // TODO does not work for git due to hashes.
                    // Instead use data / time?

                    // We may rename a file back to its old name.
                    // So only follow changes that are newer than the given revision.
                    break;
                }

                id = tmp.NewId;
                revision = tmp.NewRevision;
            }

            return id;
        }

        public void RemoveItemsWithMoreThanOneCopies()
        {
            // Skip all items where we copy into multiple files.
            _ids = _ids.Where(x => x.Value.HasMoreThanOneCopies == false).ToDictionary(x => x.Key, x => x.Value);
        }

        internal void Clear()
        {
            _ids.Clear();
        }

        /// <summary>
        /// Captures move as well as rename detail.
        /// </summary>
        private sealed class MoveInfo
        {
            public MoveInfo(NumberId newRevision, Id newId, NumberId oldRevision, Id oldId)
            {
                NewId = newId;
                OldId = oldId;
                NewRevision = newRevision;
                OldRevision = oldRevision;
            }

            public bool HasMoreThanOneCopies { get; set; }
            public Id NewId { get; }
            public NumberId NewRevision { get; }

            public Id OldId { get; }
            public NumberId OldRevision { get; }
        }
    }
}