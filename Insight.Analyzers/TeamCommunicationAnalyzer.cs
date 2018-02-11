using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;

namespace Insight.Analyzers
{
    public class TeamCommunicationAnalyzer
    {
        private readonly Dictionary<string, int> _notClassifiedDevelopers = new Dictionary<string, int>();

        /// <summary>
        /// returns the team communication path.
        /// If no team classifier is provided, the developer himself is used.
        /// </summary>
        public Dictionary<OrderedPair, int> AnalyzeTeamCommunication(ChangeSetHistory history, ITeamClassifier teamClassifier)
        {
            // file -> dictionary{team, #commits}
            var fileToCommitsPerTeam = new Dictionary<Id, Dictionary<string, int>>();

            foreach (var cs in history.ChangeSets)
            {
                // Associated team (or developer)
                var team = GetTeam(cs, teamClassifier);

                foreach (var item in cs.Items)
                {
                    // Add team to file
                    if (!fileToCommitsPerTeam.Keys.Contains(item.Id))
                    {
                        fileToCommitsPerTeam.Add(item.Id, new Dictionary<string, int>());
                    }

                    var commitsPerTeam = fileToCommitsPerTeam[item.Id];
                    commitsPerTeam.AddToValue(team, 1);
                }
            }

            // We know for each file which team did how many changes
            // Each file that was accessed by two teams leads to a link between the teams.

            // pair of teams -> number of commits.
            var teamCommunicationPaths = new Dictionary<OrderedPair, int>();

            foreach (var file in fileToCommitsPerTeam)
            {
                var currentFileTeams = file.Value.Keys.ToList();

                // Build team subsets that get counted as one path.
                for (var index = 0; index < currentFileTeams.Count - 1; index++)
                {
                    for (var index2 = 1; index2 < currentFileTeams.Count; index2++)
                    {
                        var teamPair = new OrderedPair(currentFileTeams[index], currentFileTeams[index2]);

                        // The team combination that worked together at least one time at the same file
                        // gets a point.
                        teamCommunicationPaths.AddToValue(teamPair, 1);
                    }
                }
            }

            return teamCommunicationPaths;
        }

        private string GetTeam(ChangeSet cs, ITeamClassifier teamClassifier)
        {
            var team = cs.Committer;

            if (teamClassifier != null)
            {
                team = teamClassifier.GetAssociatedTeam(cs.Committer, cs.Date);
                if (team == "NOT_CLASSIFIED")
                {
                    Trace.WriteLine("Not classified developer: " + cs.Committer + " " + cs.Date.ToShortDateString());
                    _notClassifiedDevelopers.AddToValue(cs.Committer, 1);
                }
            }

            return team;
        }
    }
}