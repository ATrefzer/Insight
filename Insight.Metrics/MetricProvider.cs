﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Insight.Metrics
{
    /// <summary>
    ///     Facade to the metrics provided by this assembly.
    /// </summary>
    public sealed class MetricProvider : IMetricProvider
    {
        private const string Cloc = "cloc-1.98.exe";
        private const string Url = "https://github.com/AlDanial/cloc/releases/download/v1.98/cloc-1.98.exe";
        private const string ClocSubDir = "ExternalTools";

        public LinesOfCode CalculateLinesOfCode(FileInfo file)
        {
            var pathToCloc = GetPathToCloc();

            var metric = new LinesOfCodeMetric(pathToCloc);
            return metric.CalculateLinesOfCode(file);
        }


        public Dictionary<string, LinesOfCode> CalculateLinesOfCode(DirectoryInfo rootDir,
                                                                    IEnumerable<string> normalizedFileExtensions)
        {
            var pathToCloc = GetPathToCloc();

            var metric = new LinesOfCodeMetric(pathToCloc);
            return metric.CalculateLinesOfCode(rootDir, normalizedFileExtensions);
        }


        public InvertedSpace CalculateInvertedSpaceMetric(FileInfo file)
        {
            var ism = new InvertedSpaceMetric();
            return ism.CalculateInvertedSpaceMetric(file);
        }


        public Dictionary<string, LinesOfCode> QueryLinesOfCode(string cacheDirectory)
        {
            var metricsFile = Path.Combine(cacheDirectory, "metrics.json");
            if (!File.Exists(metricsFile))
            {
                throw new FileNotFoundException(metricsFile);
            }

            var json = File.ReadAllText(metricsFile, Encoding.UTF8);
            return JsonConvert.DeserializeObject<Dictionary<string, LinesOfCode>>(json);
        }

        public void UpdateLinesOfCodeCache(string startDirectory, string cacheDirectory,
                                           IEnumerable<string> normalizedFileExtensions)
        {
            var metricsFile = Path.Combine(cacheDirectory, "metrics.json");

            if (File.Exists(metricsFile))
            {
                File.Delete(metricsFile);
            }

            var metric = new LinesOfCodeMetric(GetPathToCloc());

            // Take every file that can we can calculate a metric for.         
            var metrics = metric.CalculateLinesOfCode(new DirectoryInfo(startDirectory), normalizedFileExtensions);


            var json = JsonConvert.SerializeObject(metrics, Formatting.Indented);
            File.WriteAllText(metricsFile, json, Encoding.UTF8);
        }

        private string GetPathToCloc()
        {
            // Get path of this assembly - not for self contained executables.
            //var assembly = Assembly.GetAssembly(typeof(MetricProvider));
            //var assemblyDirectory = new FileInfo(assembly.Location).Directory;
            //var thisAssemblyDirectory = assemblyDirectory?.FullName ?? "";

            var thisAssemblyDirectory = System.AppContext.BaseDirectory;
            var externalToolsDirectory = Path.Combine(thisAssemblyDirectory, ClocSubDir);

            VerifyClocInstalled(externalToolsDirectory);

            return Path.Combine(externalToolsDirectory, Cloc);
        }

        private void VerifyClocInstalled(string externalToolsDirectory)
        {
            var pathToCloc = Path.Combine(externalToolsDirectory, Cloc);
            if (!File.Exists(pathToCloc))
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Executable not found: '{pathToCloc}'.");
                builder.AppendLine($"Please download '{Cloc}' from here: '{Url}'");
                builder.AppendLine($"Then copy this file to '{externalToolsDirectory}'.");
                throw new Exception(builder.ToString());
            }
        }
    }
}