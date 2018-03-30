using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Insight.Shared.Model;

namespace Insight.Shared.VersionControl
{
    /// <summary>
    /// We process the change sets from latest to oldest.
    /// Assigns each file a unique id that survives all renames.
    /// Note:
    /// Several cases with svn are converted.
    /// If we find a source file added / renamed / copied into many others this is considered as add operation.
    /// The tracking stops there because the idea of a unique file id does not apply to the concept of renaming a file into
    /// many.
    /// </summary>
    public sealed class MovementTracker
    {
        private readonly List<ChangeItem> _changeItems = new List<ChangeItem>();
        private readonly Dictionary<string, string> _serverPathToId = new Dictionary<string, string>();

        private ChangeSet _cs;

        public List<WarningMessage> Warnings = new List<WarningMessage>();

        /// <summary>
        /// Applies the ids to all items in the changeset.
        /// </summary>
        public void ApplyChangeSet(List<ChangeItem> items)
        {
            ApplyIds();
            items.AddRange(_changeItems);
            _changeItems.Clear();
        }

        public void BeginChangeSet(ChangeSet cs)
        {
            _cs = cs;
            _changeItems.Clear();
        }

        /// <summary>
        /// Requires kind and serverpath to calculate the id.
        /// The previousServerPath is only given on rename operations.
        /// </summary>
        public void TrackId(ChangeItem changeItem)
        {
            ValidateArguments(changeItem);

            // Record all changes and evaluate later when the whole change set is known.
            _changeItems.Add(changeItem);
        }

        private static void ValidateArguments(ChangeItem changeItem)
        {
            if (changeItem.ServerPath == null)
            {
                throw new ArgumentException(nameof(changeItem.ServerPath));
            }

            if (changeItem.Kind == KindOfChange.Rename && changeItem.FromServerPath == null)
            {
                throw new ArgumentException("KindOfChange inconsistent with presence of previous server path");
            }
        }

        private void ApplyIds()
        {
            // Handling needed for svn
            ConvertRenameToAddIfSourceIsModified();
            ConvertMultipleCopiesIntoAdd();
            ConvertAddDeleteToRename();

            // All other Add operations with an FromServerPath set are handled as regular add. We stop tracking here.
            // A single add is a copy from somwhere else. We keep the source, so the added file gets a new id.

            foreach (var item in _changeItems)
            {
                if (item.IsDelete())
                {
                    // We need a new id in all cases.
                    // So far we may have worked on a file that shared the same location as this deleted file
                    _serverPathToId.Remove(item.ServerPath);
                    item.Id = GetOrCreateId(item.ServerPath);
                }
                else if (item.IsEdit())
                {
                    item.Id = GetOrCreateId(item.ServerPath);
                }
                else if (item.IsAdd())
                {
                    item.Id = GetOrCreateId(item.ServerPath);

                    // Everything before the add requires gets a new id.
                    _serverPathToId.Remove(item.ServerPath);
                }
                else if (item.IsRename())
                {
                    // This is the commit where we renamed previousServerPath to changeItem.ServerPath (current name)
                    // In all commits following (older) the previousServerPath is mapped to the already used id.
                    // For all items  reuse the already known id. 
                    var id = GetOrCreateId(item.ServerPath);
                    item.Id = id;

                    _serverPathToId.Remove(item.ServerPath);

                    if (_serverPathToId.ContainsKey(item.FromServerPath) == false)
                    {
                        // Assume rename because we did not use the file in future (yet).
                        Debug.Assert(item.FromServerPath != null);
                        _serverPathToId.Add(item.FromServerPath, id);
                    }
                    else
                    {
                        // TODO
                        // If the file was modified in future the rename was an copy instead!
                        item.Kind = KindOfChange.Add;

                        var msg = $"Convert rename to add because source is modified later: '{item.ServerPath}' (from '{item.FromServerPath}')";
                        Warnings.Add(new WarningMessage(_cs.Id.ToString(), msg));
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        /// <summary>
        /// Svn can model rename or move with add and delete operations. If we find one add / delete pair this is tracked as move.
        /// The delete operation in this case does not get an id. It is removed and the add is changed to an rename operation.
        /// </summary>
        private void ConvertAddDeleteToRename()
        {
            // Hidden moves consisting of an unique add and delete pair. I convert this to a single rename operation.
            var addWithServerFromPath = _changeItems.Where(a => a.IsAdd() && a.FromServerPath != null).ToList();

            var allDeletes = _changeItems.Where(d => d.IsDelete()).ToList();

            var convertToRename = new List<ChangeItem>();
            var deletesToRemove = new List<ChangeItem>();
            foreach (var item in addWithServerFromPath)
            {
                var copies = addWithServerFromPath.Where(a => a.FromServerPath == item.FromServerPath).ToList();

                // Is there exactly one delete for the from path and no second copy?
                var deletes = allDeletes.Where(d => d.ServerPath == item.FromServerPath).ToList();

                if (copies.Count == 1 && deletes.Count == 1)
                {
                    // We can convert this to a move. The source was copied into one file and then deleted.
                    deletesToRemove.AddRange(deletes);
                    convertToRename.AddRange(copies);

                    var msg = $"Convert add/delete pair to rename: '{item.ServerPath}' (from '{item.FromServerPath}')";
                    Warnings.Add(new WarningMessage(_cs.Id.ToString(), msg));
                }
            }

            foreach (var item in convertToRename)
            {
                item.Kind = KindOfChange.Rename;
            }

            foreach (var remove in deletesToRemove)
            {
                _changeItems.Remove(remove);
            }
        }

        private void ConvertMultipleCopiesIntoAdd()
        {
            var copies = _changeItems.Where(r => r.IsRename() || r.IsAdd() && r.FromServerPath != null).ToList();

            var fromServerPaths = copies.Select(c => c.FromServerPath).Distinct();
            foreach (var fromServerPath in fromServerPaths)
            {
                var itemsWithSameFromPath = copies.Where(c => c.FromServerPath == fromServerPath).ToList();
                if (itemsWithSameFromPath.Count > 1)
                {
                    // This from server path is used more than once!
                    foreach (var item in itemsWithSameFromPath)
                    {
                        item.Kind = KindOfChange.Add;
                        var msg = $"Convert multiple copied file to add: '{item.ServerPath}' (from '{item.FromServerPath}')";
                        Warnings.Add(new WarningMessage(_cs.Id.ToString(), msg));
                    }
                }
            }
        }

        /// <summary>
        /// If we have a renamed file that was modifed in the same commit.
        /// The rename is actually a copy in this case.
        /// </summary>
        private void ConvertRenameToAddIfSourceIsModified()
        {
            var renames = _changeItems.Where(item => item.IsRename()).ToList();
            var edits = _changeItems.Where(item => item.IsEdit()).ToList();

            // The renamed file is modified in the same changeset. Consider this as an add operation because I've seen cases where
            // the source file was modified in upcoming commits.
            var convertToAdds = renames.Where(r => edits.Any(e => r.FromServerPath == e.ServerPath));

            foreach (var item in convertToAdds)
            {
                item.Kind = KindOfChange.Add;
                var msg = $"Convert rename to add because file was modified: '{item.ServerPath}' (from '{item.FromServerPath}')";
                Warnings.Add(new WarningMessage(_cs.Id.ToString(), msg));
            }
        }

        private string CreateId(string serverPath)
        {
            var uuid = Guid.NewGuid();
            var id = uuid.ToString();
            _serverPathToId.Add(serverPath, id);
            return id;
        }      

        private string GetOrCreateId(string serverPath)
        {
            string id;
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
    }
}