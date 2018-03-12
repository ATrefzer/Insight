namespace Insight.Shared
{
    // TODO read from config file
    public static class Thresholds
    {
        /// <summary>
        ///     Changesets with more modifications are ignored.
        /// </summary>
        public static int MaxItemsInChangesetForChangeCoupling = 300;

        public static int MaxWorkItemsPerCommitForSummary = 200;
        public static int MinCommitsForHotspots = 3;

        /// <summary>
        /// Reduces the output for change coupling
        /// </summary>
        public static int MinCouplingForChangeCoupling = 2;

        public static int MinLinesOfCodeForHotspot = 0;
    }
}