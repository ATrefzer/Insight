using System;
using System.Collections.Generic;
using System.Diagnostics;

using Insight.Shared.Model;

namespace Insight.GitProvider
{
    /// <summary>
    /// TODO Use this mechanism also for Svn
    /// // TODO copy into multiple files
    /// We process the change sets from latest to oldes.
    /// </summary>
    public sealed class MovementTracker
    {
        private readonly Dictionary<string, Id> _serverPathToId = new Dictionary<string, Id>();

        /// <summary>
        /// We track all move operations within an changeset and apply them when the whole changeset was handled.
        /// </summary>
        private readonly List<Move> _moves = new List<Move>();

        public void BeginChangeSet()
        {
            // Nothing to do yet.
        }

        private Id CreateId(string serverPath)
        {
            var uuid = Guid.NewGuid();
            var id = new StringId(uuid.ToString());
            _serverPathToId.Add(serverPath, id);
            return id;
        }

        public void EndChangeSet()
        {
            ApplyMoves();
        }

        Id GetOrCreateId(string serverPath)
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

        /// <summary>
        /// Requires kind and serverpath to calculate the id.
        /// The previousServerPath is only given on rename operations.
        /// </summary>
        public void SetId(ChangeItem changeItem, string previousServerPath)
        {
            ValidateArguments(changeItem, previousServerPath);

            if (changeItem.Kind == KindOfChange.Add)
            {
                changeItem.Id = GetOrCreateId(changeItem.ServerPath);

                // Everything before the add requires gets a new id.
                _serverPathToId.Remove(changeItem.ServerPath);
            }
            else if (changeItem.Kind == KindOfChange.Edit)
            {
                changeItem.Id = GetOrCreateId(changeItem.ServerPath);
            }
            else if (changeItem.Kind == KindOfChange.Delete)
            {
                // We need a new id in all cases.
                // So far we may have worked on a file that shared the same location as this deleted file
                _serverPathToId.Remove(changeItem.ServerPath);
                changeItem.Id = GetOrCreateId(changeItem.ServerPath);
            }
            else if (changeItem.Kind == KindOfChange.Rename) // Or Move
            {
                // This is the commit where we renamed previousServerPath to changeItem.ServerPath (current name)
                // In all commits following (older) the previousServerPath is mapped to the already used id.
                // For all items  reuse the already known id. 
                var id = GetOrCreateId(changeItem.ServerPath);
                changeItem.Id = id;
                _moves.Add(new Move(previousServerPath, changeItem.ServerPath, id));
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
                throw new ArgumentException(nameof(changeItem.ServerPath));
            if (!((previousServerPath == null && changeItem.Kind != KindOfChange.Rename) ||
                (previousServerPath != null && changeItem.Kind == KindOfChange.Rename)))
            {
                throw new ArgumentException("KindOfChange inconsistent with presence of previous server path");
            }
        }

        private void ApplyMoves()
        {
            foreach (var move in _moves)
            {
                _serverPathToId.Remove(move.ToServerPath);

                Debug.Assert(move.FromServerPath != null);
                if (move.FromServerPath != null)
                {
                    _serverPathToId.Add(move.FromServerPath, move.Id);
                }
            }

            _moves.Clear();
        }


        private sealed class Move
        {
            public Move(string fromServerPath, string toServerPath, Id id)
            {
                ToServerPath = toServerPath;
                FromServerPath = fromServerPath;
                Id = id;
            }

            public Id Id { get; }
            public string ToServerPath { get; }
            public string FromServerPath { get; }
        }
    }
}
