using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Insight.Shared.Model;

namespace Insight.Shared.VersionControl
{
    /// <summary>
    /// We process the change sets from latest to oldes.
    /// </summary>
    public sealed class MovementTracker
    {
        private readonly List<Add> _adds = new List<Add>();
        private readonly List<Delete> _deletes = new List<Delete>();

        private readonly List<Edit> _edits = new List<Edit>();

        /// <summary>
        /// We track all move operations within an changeset and apply them when the whole changeset was handled.
        /// </summary>
        private readonly List<Move> _moves = new List<Move>();

        private readonly Dictionary<string, Id> _serverPathToId = new Dictionary<string, Id>();

        public void BeginChangeSet()
        {
            ClearChangeSetOperations();
        }

        /// <summary>
        /// Applies the ids to all items in the changeset.
        /// </summary>
        public void ApplyChangeSet()
        {
            ApplyMoves();
            ClearChangeSetOperations();
        }

        /// <summary>
        /// Requires kind and serverpath to calculate the id.
        /// The previousServerPath is only given on rename operations.
        /// </summary>
        public void TrackId(ChangeItem changeItem, string previousServerPath)
        {
            ValidateArguments(changeItem, previousServerPath);

            if (changeItem.Kind == KindOfChange.Add)
            {
                _adds.Add(new Add(changeItem));
            }
            else if (changeItem.Kind == KindOfChange.Edit)
            {
                _edits.Add(new Edit(changeItem));
            }
            else if (changeItem.Kind == KindOfChange.Delete)
            {
                _deletes.Add(new Delete(changeItem));
            }
            else if (changeItem.Kind == KindOfChange.Rename) // Or Move
            {
                _moves.Add(new Move(changeItem, previousServerPath, changeItem.ServerPath));
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private static void ValidateArguments(ChangeItem changeItem, string previousServerPath)
        {
            // Previous server path should be exactly present if we rename.
            if (changeItem.ServerPath == null)
            {
                throw new ArgumentException(nameof(changeItem.ServerPath));
            }

            if (!(previousServerPath == null && changeItem.Kind != KindOfChange.Rename ||
                  previousServerPath != null && changeItem.Kind == KindOfChange.Rename))
            {
                throw new ArgumentException("KindOfChange inconsistent with presence of previous server path");
            }
        }

        private void ApplyMoves()
        {
            // In svn a move can consist of add and delete. We only handle deletes if not part of a rename.
            var deletes = _deletes.Where(DeleteOnly).ToList();

            foreach (var delete in deletes)
            {
                // We need a new id in all cases.
                // So far we may have worked on a file that shared the same location as this deleted file
                _serverPathToId.Remove(delete.ServerPath);
                delete.Item.Id = GetOrCreateId(delete.ServerPath);
            }

            foreach (var edit in _edits)
            {
                edit.Item.Id = GetOrCreateId(edit.ServerPath);
            }

            foreach (var add in _adds)
            {
                add.Item.Id = GetOrCreateId(add.ServerPath);

                // Everything before the add requires gets a new id.
                _serverPathToId.Remove(add.ServerPath);
            }

            foreach (var move in _moves)
            {
                // This is the commit where we renamed previousServerPath to changeItem.ServerPath (current name)
                // In all commits following (older) the previousServerPath is mapped to the already used id.
                // For all items  reuse the already known id. 
                var id = GetOrCreateId(move.ToServerPath);
                move.Item.Id = id;

                _serverPathToId.Remove(move.ToServerPath);

                Debug.Assert(move.FromServerPath != null);
                _serverPathToId.Add(move.FromServerPath, id);
            }

            _moves.Clear();
        }

        private void ClearChangeSetOperations()
        {
            _adds.Clear();
            _moves.Clear();
            _deletes.Clear();
            _edits.Clear();
        }

        private Id CreateId(string serverPath)
        {
            var uuid = Guid.NewGuid();
            var id = new StringId(uuid.ToString());
            _serverPathToId.Add(serverPath, id);
            return id;
        }

        private bool DeleteOnly(Delete delete)
        {
            // The delete is alone and does not belong to an add to form a move.
            return !_moves.Any(move => move.FromServerPath == delete.ServerPath) &&
                   !_adds.Any(add => add.ServerPath == delete.ServerPath);
        }

        private Id GetOrCreateId(string serverPath)
        {
            Id id;
            if (!_serverPathToId.ContainsKey(serverPath))
            {
                // Not seen this file before. Create a new identifier
                id = CreateId(serverPath);
            }
            else
            {
                // Id is already know.
                id = _serverPathToId[serverPath];
            }

            return id;
        }

        private class Add
        {
            public Add(ChangeItem item)
            {
                Item = item;
            }

            public ChangeItem Item { get; }

            public string ServerPath => Item.ServerPath;
        }

        private class Delete
        {
            public Delete(ChangeItem item)
            {
                Item = item;
            }

            public ChangeItem Item { get; }

            public string ServerPath => Item.ServerPath;
        }


        private class Edit
        {
            public Edit(ChangeItem item)
            {
                Item = item;
            }

            public ChangeItem Item { get; }

            public string ServerPath => Item.ServerPath;
        }

        private sealed class Move
        {
            public Move(ChangeItem item, string fromServerPath, string toServerPath)
            {
                Item = item;
                ToServerPath = toServerPath;
                FromServerPath = fromServerPath;
            }

            public string FromServerPath { get; }
            public ChangeItem Item { get; }


            public string ToServerPath { get; }
        }
    }
}