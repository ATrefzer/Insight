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
    internal sealed class MovementTracker
    {
        private readonly Dictionary<string, Id> _serverPathToId = new Dictionary<string, Id>();

        /// <summary>
        /// We track all move operations within an changeset and apply them when the whole changeset was handled.
        /// </summary>
        private readonly List<Move> _moves = new List<Move>();

        public void BeginChangeSet(ChangeSet cs)
        {
            // Nothing to do yet.
        }

        public Id CreateId(string serverPath)
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

        public void SetId(ChangeItem changeItem, string previousServerPath)
        {
            if (changeItem.Kind == KindOfChange.Add || changeItem.Kind == KindOfChange.Edit || changeItem.Kind == KindOfChange.Delete)
            {
                if (!_serverPathToId.ContainsKey(changeItem.ServerPath))
                {
                    // Not seen this file before. Create a new identifier
                    var id = CreateId(changeItem.ServerPath);
                    changeItem.Id = id;
                }
                else
                {
                    // Id is already know.
                    changeItem.Id = _serverPathToId[changeItem.ServerPath];
                }
            }
            else if(changeItem.Kind == KindOfChange.Rename) // Or Move
            {
                // This is the place back in time where we renamed the item to its current name
                var currentServerPath = changeItem.ServerPath;
                if (!_serverPathToId.ContainsKey(currentServerPath))
                {
                    // Renaming was the last action done in the repository.
                    var id = CreateId(currentServerPath);
                    changeItem.Id = id;
                }
                else
                {
                    var id = _serverPathToId[currentServerPath];
                    changeItem.Id = id;

                    // For all items following (older) reuse the already known id. 
                    _moves.Add(new Move(previousServerPath, currentServerPath, id));
                }
            }
          
            else
            {
                Debug.Assert(false);
            }
        }

        private void ApplyMoves()
        {
            foreach (var move in _moves)
            {
                _serverPathToId.Remove(move.NewServerPath);

                if (move.OldServerPath != null)
                {
                    _serverPathToId.Add(move.OldServerPath, move.Id);
                }
                else
                {
                    // We added the file here, so there is no previous modification.
                    // therefore we just remove the id. Maybe another file was stored at
                    // the location and wants a new id.
                }
            }

            _moves.Clear();
        }

        private sealed class Move
        {
            public Move(string oldServerPath, string newServerPath, Id id)
            {
                NewServerPath = newServerPath;
                OldServerPath = oldServerPath;
                Id = id;
            }

            public Id Id { get; }
            public string NewServerPath { get; }
            public string OldServerPath { get; }
        }
    }
}