namespace Insight.Shared.VersionControl
{
    public sealed class WarningMessage
    {
        public WarningMessage(string commit, string msg)
        {
            Commit = commit;
            Warning = msg;
        }

        public string Commit { get; private set; }
        public string Warning { get; private set; }
    }
}