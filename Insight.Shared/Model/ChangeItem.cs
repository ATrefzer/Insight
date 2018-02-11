using System;
using System.IO;

namespace Insight.Shared.Model
{
    [Serializable]
    [Flags]
    public enum KindOfChange // Cloned from ChangeType
    {
        Add = 2,
        Branch = 128,
        Delete = 32,
        Edit = 4,
        Encoding = 8,
        Lock = 512,
        Merge = 256,
        None = 1,
        Property = 8192,
        Rename = 16,
        Rollback = 1024,
        SourceRename = 2048,
        Undelete = 64
    }

    [Serializable]
    public sealed class ChangeItem
    {
        public Id Id { get; set; }

        public KindOfChange Kind { get; set; }
        public string LocalPath { get; set; }

        /// <summary>
        ///     Name on server. Later mapped to a local file.
        /// </summary>
        public string ServerPath { get; set; }

        public bool Exists()
        {
            return File.Exists(LocalPath);
        }

        public bool IsAdd()
        {
            return (Kind & KindOfChange.Add) == KindOfChange.Add;
        }

        public bool IsDelete()
        {
            return (Kind & KindOfChange.Delete) == KindOfChange.Delete;
        }

        public bool IsEdit()
        {
            return (Kind & KindOfChange.Edit) == KindOfChange.Edit;
        }

        public bool IsMerge()
        {
            return (Kind & KindOfChange.Merge) == KindOfChange.Merge;
        }

        public bool IsRename()
        {
            return (Kind & KindOfChange.Rename) == KindOfChange.Rename;
        }
    }
}