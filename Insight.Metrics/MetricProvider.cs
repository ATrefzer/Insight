using System.Collections.Generic;
using System.IO;
using System.Text;
using Insight.Shared;
using Insight.Shared.Model;
using Newtonsoft.Json;

namespace Insight.Metrics
{
    /// <summary>
    /// Facade to the metrics provided in the module
    /// </summary>
    public sealed class MetricProvider
    {
        private readonly string _metricsFile;
        private readonly IEnumerable<string> _normalizedFileExtensions;
        private readonly string _startDirectory;


        public MetricProvider(string projectBase, string cache, IEnumerable<string> normalizedFileExtensions)
        {
            _startDirectory = projectBase;
            _normalizedFileExtensions = normalizedFileExtensions;
            _metricsFile = Path.Combine(cache, "metrics.json");
        }

        public Dictionary<string, LinesOfCode> QueryCodeMetrics()
        {
            if (!File.Exists(_metricsFile))
            {
                throw new FileNotFoundException(_metricsFile);
            }

            var json = File.ReadAllText(_metricsFile, Encoding.UTF8);
            return JsonConvert.DeserializeObject<Dictionary<string, LinesOfCode>>(json);
        }

        public void UpdateCache()
        {
            if (File.Exists(_metricsFile))
            {
                File.Delete(_metricsFile);
            }

            // Query code metrics of local version
            var metric = new CodeMetrics();

            // Take every file that can we can calculate a metric for.         
            var metrics = metric.CalculateLinesOfCode(new DirectoryInfo(_startDirectory), _normalizedFileExtensions);

         
            var json = JsonConvert.SerializeObject(metrics, Formatting.Indented);
            File.WriteAllText(_metricsFile, json, Encoding.UTF8);
           
        }
    }
}