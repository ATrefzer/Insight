using Insight.Shared.Model;

namespace Insight.Dto
{
    public sealed class DataGridFriendlyArtifact
    {
        public int Commits { get; set; }
        public int Committers { get; set; }
        public int LOC { get; set; }
        public string LocalPath { get; set; }
        public Id Revision { get; set; }
        public int WorkItems { get; set; }
        public int CodeAge_Days { get; set; }
    }

   
}