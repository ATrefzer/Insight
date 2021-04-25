using Insight.Shared.Ui;

namespace Insight.Dto
{
    /// <summary>
    /// DO NOT FORMAT. Defines order in grid view.
    /// </summary>
    public sealed class DataGridFriendlyArtifactBasic : ICanMatch
    {
        //public string Id { get; set; }
        public string LocalPath { get; set; }
        public int Commits { get; set; }
        public int Committers { get; set; }
        public int WorkItems { get; set; }
        public int LOC { get; set; }
        public double Hotspot { get; internal set; }
        public int CodeAge_Days { get; set; }


        public bool IsMatch(string lowerCaseSearchText)
        {
            return LocalPath.ToLowerInvariant().Contains(lowerCaseSearchText);
        }
    }
}