using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Insight.Analyzers
{
    public sealed class HotspotPredictor
    {
        private readonly string _newSummary;
        private readonly string _oldSummary;

        public HotspotPredictor(string oldSummary, string newSummary)
        {
            _oldSummary = oldSummary;
            _newSummary = newSummary;
        }

        public List<HotspotDelta> GetHotspotDelta()
        {
            var result = new List<HotspotDelta>();
            var oldFileToHotspot = ParseSummaryCsv(_oldSummary);
            var newFileToHotspot = ParseSummaryCsv(_newSummary);

            // Select all files existing in both dictionaries
            var intersect = oldFileToHotspot.Keys.Intersect(newFileToHotspot.Keys);

            foreach (var localPath in intersect)
            {
                var hotspotDelta = newFileToHotspot[localPath] - oldFileToHotspot[localPath];
                result.Add(new HotspotDelta { LocalPath = localPath, Delta = hotspotDelta });
            }

            return result;
        }

        private static int GetHotspotIndex(List<string> captions)
        {
            var hotspotIndex = captions.IndexOf("Hotspot");
            if (hotspotIndex == -1)
            {
                throw new Exception("Summary does not contain hotspot information");
            }

            return hotspotIndex;
        }

        private static int GetLocalPathIndex(List<string> captions)
        {
            var pathIndex = captions.IndexOf("LocalPath");
            if (pathIndex == -1)
            {
                throw new Exception("Summary does not contain path information");
            }

            return pathIndex;
        }

        // TODO move to csv
        private Dictionary<string, double> ParseSummaryCsv(string oldSummary)
        {
            var fileToHotspot = new Dictionary<string, double>();
            using (var reader = new StreamReader(oldSummary, Encoding.UTF8))
            {
                var header = reader.ReadLine();
                if (header == null)
                {
                    throw new Exception("No header");
                }

                var captions = header.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                var hotspotIndex = GetHotspotIndex(captions);
                var pathIndex = GetLocalPathIndex(captions);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    fileToHotspot[parts[pathIndex]] = double.Parse(parts[hotspotIndex], CultureInfo.InvariantCulture);
                }
            }

            return fileToHotspot;
        }
    }
}