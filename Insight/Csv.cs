using System.Collections.Generic;

using Insight.Dto;
using Insight.Shared;
using Insight.Shared.Model;

namespace Insight
{
    public static class Csv
    {
        /// <summary>
        ///     Pair wise couplings
        /// </summary>
        public static void Write(string csvFile, List<Coupling> couplings)
        {
            var writer = new CsvWriter();
            writer.Header = true;
            writer.ToCsv(csvFile, couplings);
        }

        public static void Write(string csvFile, List<DataGridFriendlyArtifact> artifacts)
        {
            var writer = new CsvWriter();
            writer.Header = true;
            writer.ToCsv(csvFile, artifacts);
        }

        public static void Write(string csvFile, List<DataGridFriendlyComment> comments)
        {
            var writer = new CsvWriter();
            writer.Header = true;
            writer.ToCsv(csvFile, comments);
        }
    }
}