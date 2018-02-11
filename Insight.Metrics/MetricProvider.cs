using System.Collections.Generic;
using System.IO;

using Insight.Shared;
using Insight.Shared.Model;

namespace Insight.Metrics
{
    /// <summary>
    /// Facade to the metrics provided in the module
    /// </summary>
    public sealed class MetricProvider
    {
        private readonly string _metricsBinFile;
        private readonly IEnumerable<string> _normalizedFileExtensions;
        private readonly string _startDirectory;


        public MetricProvider(string projectBase, string cache, IEnumerable<string> normalizedFileExtensions)
        {
            _startDirectory = projectBase;
            _normalizedFileExtensions = normalizedFileExtensions;
            _metricsBinFile = Path.Combine(cache, "metrics.bin");
        }


        public Dictionary<string, LinesOfCode> QueryCodeMetrics()
        {
            if (!File.Exists(_metricsBinFile))
            {
                throw new FileNotFoundException(_metricsBinFile);
            }

            var binFile = new BinaryFile<Dictionary<string, LinesOfCode>>();
            return binFile.Read(_metricsBinFile);
        }

        public void UpdateCache()
        {
            if (File.Exists(_metricsBinFile))
            {
                File.Delete(_metricsBinFile);
            }

            // Query code metrics of local version
            var metric = new CodeMetrics();

            // Take every file that can we can calculate a metric for.         
            var metrics = metric.CalculateLinesOfCode(new DirectoryInfo(_startDirectory), _normalizedFileExtensions);

            var binFile = new BinaryFile<Dictionary<string, LinesOfCode>>();
            binFile.Write(_metricsBinFile, metrics);
        }
    }
}