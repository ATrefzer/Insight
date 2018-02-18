using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using CsvHelper;

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
            StreamWriter stream = null;
            try
            {
                stream = new StreamWriter(csvFile, false, Encoding.UTF8);

                // Write header
                stream.WriteLine("item1,item2,couplings,degree");

                using (var csv = new CsvWriter(stream))
                {
                    stream = null;

                    foreach (var coupling in couplings)
                    {
                        csv.WriteField(coupling.Item1);
                        csv.WriteField(coupling.Item2);
                        csv.WriteField(coupling.Couplings);
                        csv.WriteField(coupling.Degree.ToString("N2", CultureInfo.InvariantCulture));
                    }
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        public static void Write(string csvFile, IEnumerable<DataGridFriendlyArtifact> artifacts)
        {
            StreamWriter stream = null;

            try
            {
                stream = new StreamWriter(csvFile, false, Encoding.UTF8);
                using (var csv = new CsvWriter(stream))
                {
                    stream = null;
                    // Write header
                    stream.WriteLine("local_path,revision,commits,committers,loc");

                    foreach (var artifact in artifacts)
                    {
                        WriteArtefact(csv, artifact);
                    }
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        private static void WriteArtefact(CsvWriter csv, DataGridFriendlyArtifact artifact)
        {
            csv.WriteField(artifact.LocalPath);
            csv.WriteField(artifact.Revision);
            csv.WriteField(artifact.Commits);
            csv.WriteField(artifact.Committers);
            csv.WriteField(artifact.LOC);
            csv.NextRecord();
        }

        /// <summary>
        /// TODO: This gives an idea of extra information
        /// </summary>
        private static void WriteArtefact(CsvWriter csv, Artifact artifact, Dictionary<string, MainDeveloper> mainDevelopers)
        {
            csv.WriteField(artifact.Id.ToString());
            csv.WriteField(artifact.Revision);
            csv.WriteField(artifact.LocalPath);
            csv.WriteField(artifact.ServerPath);
            csv.WriteField(artifact.Commits);
            csv.WriteField(artifact.Committers.Count.ToString(CultureInfo.InvariantCulture));
            csv.WriteField(artifact.WorkItems.Count.ToString(CultureInfo.InvariantCulture));
            csv.WriteField(artifact.WorkItems.Count(workItem => workItem.IsBug()).ToString(CultureInfo.InvariantCulture));
            csv.WriteField(artifact.Teams.Count.ToString(CultureInfo.InvariantCulture));

            // Developer work is optional
            if (mainDevelopers != null && mainDevelopers.ContainsKey(artifact.LocalPath))
            {
                var mainDev = mainDevelopers[artifact.LocalPath];
                csv.WriteField(mainDev.Developer.Replace(", ", " "));
                csv.WriteField(mainDev.Percent.ToString("N2", CultureInfo.InvariantCulture));
            }
            else
            {
                csv.WriteField("unknown");
                csv.WriteField(0);
            }

            csv.NextRecord();
        }
    }
}