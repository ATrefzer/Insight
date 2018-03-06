using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Insight.Analyzers;
using Insight.Builder;
using Insight.Dto;
using Insight.Metrics;
using Insight.Shared;
using Insight.Shared.Model;
using Insight.Shared.System;
using Insight.Shared.VersionControl;

using Visualization.Controls;
using Visualization.Controls.Bitmap;

namespace Insight
{
    public sealed class Analyzer
    {
        private ChangeSetHistory _history;
        private Dictionary<string, LinesOfCode> _metrics;

        public Analyzer(Project project)
        {
            Project = project;
        }

        public List<WarningMessage> Warnings { get; private set; }

        private Project Project { get; }

        /// <summary>
        /// Work for a single file. Developer -> lines of work
        /// </summary>
        public static MainDeveloper GetMainDeveloper(Dictionary<string, int> workByDevelopers)
        {
            // Find main developer
            string mainDeveloper = null;
            double linesOfWork = 0;

            double lineCount = workByDevelopers.Values.Sum();

            foreach (var pair in workByDevelopers)
            {
                if (pair.Value > linesOfWork)
                {
                    mainDeveloper = pair.Key;
                    linesOfWork = pair.Value;
                }
            }

            return new MainDeveloper(mainDeveloper, 100.0 * linesOfWork / lineCount);
        }

        public List<Coupling> AnalyzeChangeCoupling()
        {
            LoadHistory();

            // Pair wise couplings
            var tmp = new ChangeCouplingAnalyzer();
            var couplings = tmp.CalculateChangeCouplings(_history, Project.Filter);
            var sortedCouplings = couplings.OrderByDescending(coupling => coupling.Degree).ToList();
            Csv.Write(Path.Combine(Project.Cache, "change_couplings.csv"), sortedCouplings);

            // Same with classified folders
            var classifiedCouplings = tmp.CalculateChangeCouplings(_history, localPath => { return ClassifyDirectory(localPath); });
            Csv.Write(Path.Combine(Project.Cache, "classified_change_couplings.csv"), classifiedCouplings);

            return sortedCouplings;
        }

        public HierarchicalDataContext AnalyzeCodeAge()
        {
            LoadHistory();
            LoadMetrics();

            // Get summary of all files
            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));

            var builder = new CodeAgeBuilder();
            var hierarchicalData = builder.Build(summary, _metrics);
            return new HierarchicalDataContext(hierarchicalData);
        }

        public HierarchicalDataContext AnalyzeHotspots()
        {
            LoadHistory();
            LoadMetrics();

            // Get summary of all files
            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));

            var builder = new HotspotBuilder();
            var hierarchicalData = builder.Build(summary, _metrics);
            return new HierarchicalDataContext(hierarchicalData);
        }

        public HierarchicalDataContext AnalyzeKnowledge(string directory)
        {
            var scanner = new DirectoryScanner();
            var filesToAnalyze = scanner.GetFilesRecursive(directory);

            // With git we have all files locally. But think first before requesting many thousand files from the Svn server.
            if (MessageBox.Show($"The folder contains {filesToAnalyze.Count} files to analyze. Really?", "Really?", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return null;
            }

            // Get summary of all files within the selected direcory
            LoadHistory();
            LoadMetrics();

            // Extend the default filter to only accept files from the given directory.
            // This is faster than summary.Where() because we skip a lot of File.Exist() calls.
            var newFilter = new Filter(Project.Filter, new FileFilter(filesToAnalyze));
            var summary = _history.GetArtifactSummary(newFilter, new HashSet<string>(_metrics.Keys));

            // We have a summary of just a sub directory with a limited amount of files.
            // I don't want to download that many files from the server.

            // Calculate main developer for each file
            var mainDeveloperPerFile = new ConcurrentDictionary<string, MainDeveloper>();

            Parallel.ForEach(summary, new ParallelOptions { MaxDegreeOfParallelism = 4 },
                             artifact =>
                             {
                                 var provider = Project.CreateProvider();

                                 var work = provider.CalculateDeveloperWork(artifact);
                                 var mainDeveloper = GetMainDeveloper(work);
                                 mainDeveloperPerFile.TryAdd(artifact.LocalPath, mainDeveloper);
                             });

            // Assign a color to each developer
            var developers = mainDeveloperPerFile.Select(pair => pair.Value.Developer).Distinct();
            var scheme = new ColorScheme(developers.ToArray());

            // Build the knowledge data
            var builder = new KnowledgeBuilder();
            var hierarchicalData = builder.Build(summary, _metrics, mainDeveloperPerFile
                                                         .ToDictionary(pair => pair.Key, pair => pair.Value));

            return new HierarchicalDataContext(hierarchicalData, scheme);
        }


        public List<TrendData> AnalyzeTrend(string localFile)
        {
            var trend = new List<TrendData>();

            var svnProvider = Project.CreateProvider();

            // Svn log on this file to get all revisions
            var fileHistory = svnProvider.ExportFileHistory(localFile);

            // For each file we need to calculate the metrics
            var provider = new CodeMetrics();

            foreach (var file in fileHistory)
            {
                var fileInfo = new FileInfo(file.CachePath);
                var loc = provider.CalculateLinesOfCode(fileInfo);
                var invertedSpace = provider.CalculateInvertedSpaceMetric(fileInfo);
                trend.Add(new TrendData { Date = file.Date, Loc = loc, InvertedSpace = invertedSpace });
            }

            return trend;
        }

        /// <summary>
        /// If we call this in context of a knowledge map we reuse existing colors colors and simply add
        /// missing developers/colors. You can optionally add a uninitialized (default ctor) ColorScheme.
        /// </summary>
        public string AnalyzeWorkOnSingleFile(string fileName, ColorScheme colorScheme)
        {
            Debug.Assert(colorScheme != null);
            var provider = Project.CreateProvider();
            var workByDeveloper = provider.CalculateDeveloperWork(new Artifact { LocalPath = fileName });

            var bitmap = new FractionBitmap();

            var fi = new FileInfo(fileName);
            var path = Path.Combine(Project.Cache, fi.Name) + ".bmp";

            InitColorMappingForWork(colorScheme, workByDeveloper);

            bitmap.Create(path, workByDeveloper, colorScheme, true);

            return path;
        }

        public List<DataGridFriendlyComment> ExportComments()
        {
            /*
              R Code
              library(tm)
              library(wordcloud)

              comments = read.csv("d:\\comments.csv", stringsAsFactors=FALSE)
              names(comments) = c("comment")

              corpus = Corpus(VectorSource(comments[,1]))
              corpus = tm_map(corpus, tolower)
              #corpus = tm_map(corpus, PlainTextDocument)
              corpus = tm_map(corpus, removePunctuation)
              corpus = tm_map(corpus, removeWords, stopwords("english"))
              frequencies = DocumentTermMatrix(corpus)
              sparse = removeSparseTerms(frequencies, 0.99)
              all = as.data.frame(as.matrix(sparse))

              wordcloud(colnames(all), colSums(all))
          */

            LoadHistory();
            var result = new List<DataGridFriendlyComment>();
            foreach (var cs in _history.ChangeSets)
            {
                result.Add(new DataGridFriendlyComment
                           {
                                   Committer = cs.Committer,
                                   Comment = cs.Comment
                           });
            }

            Csv.Write(Path.Combine(Project.Cache, "comments.csv"), result);
            return result;
        }

        public List<DataGridFriendlyArtifact> ExportSummary()
        {
            LoadHistory();
            LoadMetrics();

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));

            var gridData = new List<DataGridFriendlyArtifact>();
            foreach (var artifact in summary)
            {
                var metricKey = artifact.LocalPath.ToLowerInvariant();
                var loc = _metrics.ContainsKey(metricKey) ? _metrics[metricKey].Code : 0;
                gridData.Add(
                             new DataGridFriendlyArtifact
                             {
                                     LocalPath = artifact.LocalPath,
                                     Revision = artifact.Revision,
                                     Commits = artifact.Commits,
                                     Committers = artifact.Committers.Count,
                                     LOC = loc,
                                     WorkItems = artifact.WorkItems.Count,
                                     CodeAge_Days = (DateTime.Now - artifact.Date).Days
                             });
            }

            Csv.Write(Path.Combine(Project.Cache, "summary.csv"), gridData);
            return gridData;
        }

        public void UpdateCache()
        {
            // Note: You should have the latest code locally such that history and metrics match!
            // Update svn history
            var svnProvider = Project.CreateProvider();
            svnProvider.UpdateCache();

            // Update code metrics (after source was updated)
            var metricProvider = new MetricProvider(Project.ProjectBase, Project.Cache, Project.GetNormalizedFileExtensions());
            metricProvider.UpdateCache();
        }

        internal void Clear()
        {
            _history = null;
            _metrics = null;
        }

        private static string ClassifyDirectory(string localPath)
        {
            // Classify different source code folders

            // THIS IS AN EXAMPLE
            if (localPath.Contains("UnitTest"))
            {
                return "Test";
            }

            if (localPath.Contains("UI"))
            {
                return "UserInterface";
            }

            if (localPath.Contains("bla\\bla\\bla"))
            {
                return "bla";
            }

            return string.Empty;
        }

        private void InitColorMappingForWork(ColorScheme colorMapping, Dictionary<string, int> workByDeveloper)
        {
            // order such that same developers get same colors regardless of order.
            foreach (var developer in workByDeveloper.Keys.OrderBy(x => x))
            {
                colorMapping.AddColorKey(developer);
            }
        }

        private void LoadHistory()
        {
            if (_history == null)
            {
                var provider = Project.CreateProvider();
                _history = provider.QueryChangeSetHistory();
                Warnings = provider.Warnings;

                // Remove all items that are deleted now.
                _history.CleanupHistory();
            }
        }

        private void LoadMetrics()
        {
            // Get code metrics (all files from the cache!)
            if (_metrics == null)
            {
                var metricProvider = new MetricProvider(Project.ProjectBase, Project.Cache, Project.GetNormalizedFileExtensions());
                _metrics = metricProvider.QueryCodeMetrics();
            }
        }
    }
}