namespace Insight.Dto
{
    public class DataGridFriendlyArtifactBasic
    {
        public int CodeAge_Days { get; set; }
        public int Commits { get; set; }
        public int Committers { get; set; }
        public double Hotspot { get; internal set; }
        public int LOC { get; set; }
        public string LocalPath { get; set; }
        public string Revision { get; set; }
        public int WorkItems { get; set; }
    }
}