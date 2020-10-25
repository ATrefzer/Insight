namespace Insight.GitProvider.Debugging
{
    public sealed class Statistics
    {
        public int LookupTreeAfterBothBranchesRenamedFile { get; set; }
        public int RestartWithNewFileIdBecauseAddedInDifferentBranches { get; set; }
    }
}