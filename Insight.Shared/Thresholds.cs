namespace Insight.Shared
{
    // TODO read from config file
    public static class Thresholds
    {
       

        public static int MaxWorkItemsPerCommitForSummary = 200;

        public static int MinCommitsForHotspots = 3;
        public static int MinLinesOfCodeForHotspot = 0;


        /// <summary>
        /// Changesets with more modifications are ignored in change coupling.
        /// </summary>
        public static int MaxItemsInChangesetForChangeCoupling = 300;

        /// <summary>
        /// Reduces the output for change coupling
        /// </summary>
        public static int MinCouplingForChangeCoupling = 20;
        public static double MinDegreeForChangeCoupling = 50.0;

       
    }
}