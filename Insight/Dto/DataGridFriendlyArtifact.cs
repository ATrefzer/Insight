namespace Insight.Dto
{
    /// <summary>
    /// Copied base class attributes to control order in data grid / csv
    /// </summary>
    public sealed class DataGridFriendlyArtifact
    {
        public int CodeAge_Days { get; set; }
        public int Commits { get; set; }
        public int Committers { get; set; }

        // All worker related information
        public double FractalValue { get; set; }
        public double Hotspot { get; internal set; }
        public int LOC { get; set; }
        public string LocalPath { get; set; }
        public string MainDev { get; set; }
        public double MainDevPercent { get; set; }
        public string Revision { get; set; }
        public int WorkItems { get; set; }
    }
}