using Insight.Shared.Ui;

namespace Insight.Shared.VersionControl
{
    public sealed class WarningMessage : ICanMatch
    {
        public WarningMessage(string commit, string msg)
        {
            Commit = commit;
            Warning = msg;
        }

        public string Commit { get; }
        public string Warning { get; }

        public bool IsMatch(string lowerCaseSearchText)
        {
            return Warning.ToLowerInvariant().Contains(lowerCaseSearchText);
        }
    }
}