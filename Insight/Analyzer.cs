using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Insight.Analyzers;
using Insight.Builder;
using Insight.Metrics;
using Insight.Shared;
using Insight.Shared.Model;
using Insight.Shared.System;

using Visualization.Controls;
using Visualization.Controls.Bitmap;
using Visualization.Controls.Data;

namespace Insight
{
    internal class Analyzer
    {
        private ChangeSetHistory _history;
        private Dictionary<string, LinesOfCode> _metrics;

        public Analyzer(Project project)
        {
            Project = project;
        }

        public Project Project { get; }

        public Task<HierarchicalData> AnalyzeHotspotsAsync()
        {
            return Task.Run(() =>
                            {
                                LoadHistory();
                                LoadMetrics();

                                // Get summary of all files
                                var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));

                                var builder = new HotspotBuilder();
                                var hierarchicalData = builder.Build(summary, _metrics);
                                return hierarchicalData;
                            });
        }

        public Task<HierarchicalData> AnalyzeKnowledgeAsync(string directory)
        {
            return Task.Run(() =>
                            {
                                var scanner = new DirectoryScanner();
                                var filesToAnalyze = scanner.GetFilesRecursive(directory);
                                var limit = 50;
                                if (filesToAnalyze.Count() > limit)
                                {
                                    // TODO rather throw an exception!
                                    MessageBox.Show($"The folder contains more than {limit} files. This is not allowed!");
                                    return null;
                                }

                                // Get summary of all files within the selected direcory
                                LoadHistory();
                                LoadMetrics();

                                // TODO check if metrics exist and history exist => Sync first!

                                // Extend the default filter to only accept files from the given directory.
                                // This is faster than summary.Where() because we skip a lot of File.Exist() calls.
                                var newFilter = new Filter(Project.Filter, new FileFilter(filesToAnalyze));
                                var summary = _history.GetArtifactSummary(newFilter, new HashSet<string>(_metrics.Keys));

                                // We have a summary of just a sub directory with a limited amount of files.
                                // I don't want to download that many files from the server.

                                // Calculate main developer for each file
                                var mainDeveloperPerFile = new Dictionary<string, MainDeveloper>();
                                foreach (var artifact in summary)
                                {
                                    var svnProvider = Project.CreateProvider();

                                    var work = svnProvider.CalculateDeveloperWork(artifact);
                                    var mainDeveloper = GetMainDeveloper(work);
                                    mainDeveloperPerFile.Add(artifact.LocalPath, mainDeveloper);
                                }

                                // Assign a color to each developer
                                var developers = mainDeveloperPerFile.Select(pair => pair.Value.Developer).Distinct();
                                var mapper = new NameToColorMapper(developers.ToArray());
                                ColorScheme.SetColorMapping(mapper);

                                // Build the knowledge data
                                var builder = new KnowledgeBuilder();
                                var hierarchicalData = builder.Build(summary, _metrics, mainDeveloperPerFile);
                                return hierarchicalData;
                            });
        }

        public Task<List<Coupling>> AnalyzeTemporalCouplingsAsync()
        {
            return Task.Run(() =>
                            {
                                LoadHistory();

                                // Pair wise couplings
                                var tmp = new ChangeCouplingAnalyzer();
                                var couplings = tmp.CalculateTemporalCouplings(_history, Project.Filter);
                                var sortedCouplings = couplings.OrderByDescending(coupling => coupling.Degree).ToList();
                                Csv.Write(Path.Combine(Project.Cache, "couplings.csv"), sortedCouplings);

                                // Same with classified folders
                                var classifiedCouplings = tmp.CalculateTemporalCouplings(_history, localPath => { return ClassifyDirectory(localPath); });
                                Csv.Write(Path.Combine(Project.Cache, "classified_couplings.csv"), classifiedCouplings);

                                return sortedCouplings;
                            });
        }

        public Task<string> AnalyzeWorkOnSingleFileAsync(string fileName)
        {
            return Task.Run(() =>
                            {
                                var svnProvider = Project.CreateProvider();
                                var workByDeveloper = svnProvider.CalculateDeveloperWork(new Artifact { LocalPath = fileName });

                                var bitmap = new FractionBitmap();

                                var fi = new FileInfo(fileName);
                                var path = Path.Combine(Project.Cache, fi.Name) + ".bmp";

                                // Let determine colors automatically 
                                var colorMapping = new NameToColorMapper(workByDeveloper.Keys.ToArray());
                                bitmap.Create(path, workByDeveloper, colorMapping, true);

                                return path;
                            });
        }

        public Task ExportComments()
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

            return Task.Run(() =>
                            {
                                LoadHistory();

                                var fileName = Path.Combine(Project.Cache, "comments.csv");
                                using (var file = File.CreateText(fileName))
                                {
                                    foreach (var cs in _history.ChangeSets)
                                    {
                                        file.WriteLine("\"" + cs.Comment + "\"");
                                    }
                                }
                            });
        }

        public Task<List<DataGridFriendlyArtifact>> ExportSummary()
        {
            return Task.Run(() =>
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
                                                         WorkItems = artifact.WorkItems.Count
                                                 });
                                }

                                Csv.Write(Path.Combine(Project.Cache, "Export.csv"), gridData);
                                return gridData;
                            });
        }

        /// <summary>
        /// Work for a single file. Developer -> lines of work
        /// </summary>
        public MainDeveloper GetMainDeveloper(Dictionary<string, int> workByDevelopers)
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

        public async Task UpdateCacheAsyc()
        {
            await Task.Run(() =>
                           {
                               // Note: You should have the latest code locally such that history and metrics match!
                               // Update svn history
                               var svnProvider = Project.CreateProvider();
                               svnProvider.UpdateCache();

                               // Update code metrics (after source was updated)
                               var metricProvider = new MetricProvider(Project.ProjectBase, Project.Cache, Project.GetNormalizedFileExtensions());
                               metricProvider.UpdateCache();
                           });
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

        private void LoadHistory()
        {
            if (_history == null)
            {
                var svnProvider = Project.CreateProvider();
                _history = svnProvider.QueryChangeSetHistory();
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