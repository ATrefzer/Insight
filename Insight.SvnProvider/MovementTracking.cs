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

        public void Add(ulong newRevision, Id newId, ulong oldRevision, Id oldId)
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

        public Id GetLatestId(Id oldId, ulong oldRevision)
        {
            if (!_ids.ContainsKey(oldId))
            {
                return oldId;
            }

            // Stop on cycle to new id
            var id = oldId;
            var revision = oldRevision;

            while (_ids.TryGetValue(id, out MoveInfo tmp))
            {
                // There is a newer id for the given file id.

                if (tmp.NewRevision < revision)
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
            public readonly Id NewId;
            public readonly ulong NewRevision;
            public bool HasMoreThanOneCopies;

            public MoveInfo(ulong newRevision, Id newId, ulong oldRevision, Id oldId)
            {
                NewId = newId;
                OldId = oldId;
                NewRevision = newRevision;
                OldRevision = oldRevision;
            }

            public Id OldId { get; set; }
            public ulong OldRevision { get; set; }
        }
    }
}