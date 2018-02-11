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
    internal sealed class MovementTracking
    {
        // old id -> all infos
        private Dictionary<Id, Item> _ids = new Dictionary<Id, Item>();

        public void Add(int revision, Id newId, int oldRevision, Id oldId)
        {
            Debug.Assert(newId != null);
            if (oldId.Equals(newId))
            {
                // Unglaublich, aber das passiert wirklich -> Endlosrekursion bei GetLatestId
                return;
            }

            if (_ids.ContainsKey(oldId))
            {
                // This file is "renamed" or "added" into multiple others. 
                // Seems to be possible with svn. These are only a few cases.
                var item = _ids[oldId];
                item.HasMoreThanOneCopies = true; // Removed later.
                return;
            }

            _ids.Add(oldId, new Item(newId, oldId, revision, oldRevision));
        }

        public Id GetLatestId(Id oldId, int oldRevision)
        {
            if (!_ids.ContainsKey(oldId))
            {
                return oldId;
            }

            // Stop on cycle to new id
            var id = oldId;
            var revision = oldRevision;

            Item tmp;
            while (_ids.TryGetValue(id, out tmp))
            {
                if (tmp.NewRevision < revision)
                {
                    // We may rename a file back to its old name.
                    // So only follow changes that are newer than the given revision.
                    break;
                }

                id = tmp.NewId;
                revision = tmp.NewRevision;
            }

            //if (id.Equals(oldId) == false)
            //{
            //    Debug.WriteLine(oldId + " -> " + id);
            //}

            return id;
        }

        public void RemoveInvalid()
        {
            // Skip all items where we copy into multiple files.
            _ids = _ids.Where(x => x.Value.HasMoreThanOneCopies == false).ToDictionary(x => x.Key, x => x.Value);
        }

        internal void Clear()
        {
            _ids.Clear();
        }

        private sealed class Item
        {
            public readonly Id NewId;
            public readonly int NewRevision;
            public bool HasMoreThanOneCopies;

            public Item(Id newId, Id oldId, int newRevision, int oldRevision)
            {
                NewId = newId;
                OldId = oldId;
                NewRevision = newRevision;
                OldRevision = oldRevision;
            }

            // ReSharper disable MemberCanBePrivate.Local
            // ReSharper disable NotAccessedField.Local
            public Id OldId;

            public int OldRevision;

            // ReSharper restore MemberCanBePrivate.Local
            // ReSharper restore NotAccessedField.Local
        }
    }
}