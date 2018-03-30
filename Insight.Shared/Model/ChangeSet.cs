using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Insight.Shared.Model
{
    [Serializable]
    public sealed class ChangeSet
    {
        public string Comment { get; set; }
        public string Committer { get; set; }
        public DateTime Date { get; set; }
        public string Id { get; set; }
        public List<ChangeItem> Items { get; } = new List<ChangeItem>();

        public List<WorkItem> WorkItems { get; } = new List<WorkItem>();     
    }
}