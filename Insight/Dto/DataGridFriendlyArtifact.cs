using Insight.ViewModels;

namespace Insight.Dto
{
    /// <summary>
    /// Copied base class attributes to control order in data grid / csv
    /// DO NOT FORMAT
    /// </summary>
    public sealed class DataGridFriendlyArtifact : ICanMatch
    {
        //public string Id { get; set; }
        public string LocalPath { get; set; }
        public int Commits { get; set; }
        public int Committers { get; set; }
        public int WorkItems { get; set; }
        public int LOC { get; set; }
        public double Hotspot { get; internal set; }
        public int CodeAge_Days { get; set; }

        // All worker related information
        public double FractalValue { get; set; }
        public string MainDev { get; set; }
        public double MainDevPercent { get; set; }

        public bool IsMatch(string lowerCaseSearchText)
        {
            return LocalPath.ToLowerInvariant().Contains(lowerCaseSearchText);
        }
    }
}