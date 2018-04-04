using Insight.Calculation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Insight.Shared.Model
{
    /// <summary>
    /// Contributions for a single file or logical component.
    /// </summary>
    [Serializable]
    public sealed class Contribution
    {
        public Dictionary<string, uint> DeveloperToContribution { get; }

        public Contribution(Dictionary<string, uint> developerToContribution)
        {
            DeveloperToContribution = developerToContribution;
        }

        public double CalculateFractalValue()
        {
            return FractalValue.Calculate(DeveloperToContribution);
        }

        /// <summary>
        /// Returns the main developer for a single file
        /// </summary>
        public MainDeveloper GetMainDeveloper()
        {
            // Find main developer
            string mainDeveloper = "---";
            double linesOfWork = 0;

            double lineCount = Math.Max(DeveloperToContribution.Values.Sum(w => w), 1);

            foreach (var pair in DeveloperToContribution)
            {
                if (pair.Value > linesOfWork)
                {
                    mainDeveloper = pair.Key;
                    linesOfWork = pair.Value;
                }
            }

            return new MainDeveloper(mainDeveloper, 100.0 * linesOfWork / lineCount);
        }
    }
}