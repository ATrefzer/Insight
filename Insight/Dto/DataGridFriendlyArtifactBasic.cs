using Insight.Shared.Model;

namespace Insight.Dto
{
    public class DataGridFriendlyArtifactBasic
    {
        public Id Revision { get; set; }
        public string LocalPath { get; set; }
        public int Commits { get; set; }
        public int Committers { get; set; }
        public int LOC { get; set; }
        public int WorkItems { get; set; }
        public int CodeAge_Days { get; set; }
    }
}