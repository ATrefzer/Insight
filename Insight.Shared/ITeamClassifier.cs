using System;

namespace Insight.Shared
{
    public interface ITeamClassifier
    {
        string GetAssociatedTeam(string committer, DateTime checkinDate);
    }
}