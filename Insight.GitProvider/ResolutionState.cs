using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Insight.GitProvider
{
    enum Resolution
    {
        KeepWithNewId,
        Add,
        GetExisting,
        Remove,
        Keep,
        Rename
    }

    /// <summary>
    /// When processing a commit we have to update the scope of the commit incrementally.
    /// At the end of the processing NewScope contains the final scope assigned to the node.
    /// </summary>
    class ResolutionState
    {
        public bool HasAmbiguities()
        {
            return OnlyInMergeFrom?.Any() == true || OnlyInMergeInto?.Any() == true;
        }

        public IEnumerable<KeyValuePair<string, Guid>> GetMergeAmbiguities()
        {
            if (!HasAmbiguities())
            {
                return new List<KeyValuePair<string, Guid>>();
            }

            return OnlyInMergeInto.Union(OnlyInMergeFrom);
        }

        public Scope NewScope { get; set; }

        public Scope OnlyInMergeFrom { get; set; }

        public Scope OnlyInMergeInto { get; set; }

        public string Resolve(string serverPath, Resolution resolution, string fromServerPath = null)
        {
            string id = null;
            if (resolution == Resolution.KeepWithNewId)
            {
                var newGuid = Guid.NewGuid();
                id = newGuid.ToString();
                NewScope.MergeAdd(serverPath, newGuid);
            }

            else if (resolution == Resolution.Keep)
            {
                id = GetId(serverPath);
                NewScope.MergeAdd(serverPath, Guid.Parse(id));
            }

            else if (resolution == Resolution.Add)
            {
                id = NewScope.Add(serverPath);
            }

            else if (resolution == Resolution.GetExisting)
            {
                id = GetId(serverPath);
            }
            else if (resolution == Resolution.Remove)
            {
                id = GetId(serverPath);
                NewScope.Remove(serverPath);
            }
            else if (resolution == Resolution.Rename)
            {
                id = GetId(fromServerPath);
                NewScope.Update(fromServerPath, serverPath);
                Debug.Assert(id == NewScope.GetId(serverPath));
            }
            else
            {
                Debug.Fail("Not implemented");
            }

            // Scratch from the ambiguous things.
            OnlyInMergeInto?.Remove(serverPath);
            OnlyInMergeFrom?.Remove(serverPath);

            return id;
        }

        private string GetId(string serverPath)
        {
            var id = NewScope.GetIdOrDefault(serverPath);
            if (id != null)
            {
                return id;
            }

            var isKnownInMergeInto = OnlyInMergeInto != null && OnlyInMergeInto.IsKnown(serverPath);
            var isKnownInMergeFrom = OnlyInMergeFrom != null && OnlyInMergeFrom.IsKnown(serverPath);

            if (isKnownInMergeInto && !isKnownInMergeFrom)
            {
                id = OnlyInMergeInto.GetId(serverPath);
            }
            else if (!isKnownInMergeInto && isKnownInMergeFrom)
            {
                id = OnlyInMergeFrom.GetId(serverPath);
            }
            else if (!isKnownInMergeInto && !isKnownInMergeFrom)
            {
                return null;
            }
            else
            {
                Debug.Fail("Not handled");
            }

            return id;
        }
    }
}