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
        Keep
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

        public string Resolve(string serverPath, Resolution resolution)
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
                NewScope.Remove(serverPath);
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

            if (OnlyInMergeInto.IsKnown(serverPath) && !OnlyInMergeFrom.IsKnown(serverPath))
            {
                id = OnlyInMergeInto.GetId(serverPath);
            }
            else if (!OnlyInMergeInto.IsKnown(serverPath) && OnlyInMergeFrom.IsKnown(serverPath))
            {
                id = OnlyInMergeFrom.GetId(serverPath);
            }
            else
            {
                Debug.Fail("Not handled");
            }

            return id;
        }
    }
}