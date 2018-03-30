using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Insight.Analyzers;
using Insight.Builder;
using Insight.Dto;
using Insight.Metrics;
using Insight.Shared.Extensions;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;

using Newtonsoft.Json;

using Visualization.Controls;
using Visualization.Controls.Bitmap;

namespace Insight
{
    public sealed class Analyzer
    {
        /// <summary>
        /// Local path to contribution
        /// </summary>
        Dictionary<string, Contribution> _contributions;

        ChangeSetHistory _history;
        Dictionary<string, LinesOfCode> _metrics;

        public Analyzer(Project project)
        {
            Project = project;
        }

        public List<WarningMessage> Warnings { get; private set; }

        Project Project { get; }


        public List<Coupling> AnalyzeChangeCoupling()
        {
            LoadHistory();

            // Pair wise couplings
            var tmp = new ChangeCouplingAnalyzer();
            var couplings = tmp.CalculateChangeCouplings(_history, Project.Filter);
            var sortedCouplings = couplings.OrderByDescending(coupling => coupling.Degree).ToList();
            Csv.Write(Path.Combine(Project.Cache, "change_couplings.csv"), sortedCouplings);

            // Same with classified folders
            var classifiedCouplings = tmp.CalculateClassifiedChangeCouplings(_history, localPath => { return ClassifyDirectory(localPath); });
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

        /// <summary>
        /// Analyzes the fragmentation per file.
        /// </summary>
        public HierarchicalDataContext AnalyzeFragmentation()
        {
            LoadHistory();
            LoadMetrics();
            LoadContributions();

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));
            var fileToFractalValue = _contributions.ToDictionary(pair => pair.Key, pair => pair.Value.CalculateFractalValue());

            var builder = new FragmentationBuilder();
            var hierarchicalData = builder.Build(summary, _metrics, fileToFractalValue);

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

        public HierarchicalDataContext AnalyzeKnowledge()
        {
            LoadHistory();
            LoadMetrics();
            LoadContributions();

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));
            var fileToMainDeveloper = _contributions.ToDictionary(pair => pair.Key, pair => pair.Value.GetMainDeveloper());

            // Assign a color to each developer
            var mainDevelopers = fileToMainDeveloper.Select(pair => pair.Value.Developer).Distinct();
            var scheme = new ColorScheme(mainDevelopers.ToArray());

            // Build the knowledge data
            var builder = new KnowledgeBuilder();
            var hierarchicalData = builder.Build(summary, _metrics, fileToMainDeveloper);

            return new HierarchicalDataContext(hierarchicalData, scheme);
        }

        /// <summary>
        /// Same as knowledge but uses a different color scheme
        /// </summary>
        public HierarchicalDataContext AnalyzeKnowledgeLoss(string developer)
        {
            LoadHistory();
            LoadMetrics();
            LoadContributions();

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));
            var fileToMainDeveloper = _contributions.ToDictionary(pair => pair.Key, pair => pair.Value.GetMainDeveloper());

            // Assign a color to each developer
            // Include all other developers. So we have a more consistent coloring.
            var mainDevelopers = fileToMainDeveloper.Select(pair => pair.Value.Developer).Distinct();
            var scheme = new ColorScheme(mainDevelopers.ToArray());

            // Build the knowledge data
            var builder = new KnowledgeBuilder(developer);
            var hierarchicalData = builder.Build(summary, _metrics, fileToMainDeveloper);

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

        public string AnalyzeWorkOnSingleFile(string fileName, ColorScheme colorScheme)
        {
            Debug.Assert(colorScheme != null);
            var provider = Project.CreateProvider();
            var workByDeveloper = provider.CalculateDeveloperWork(new Artifact { LocalPath = fileName });

            var bitmap = new FractionBitmap();

            var fi = new FileInfo(fileName);
            var path = Path.Combine(Project.Cache, fi.Name) + ".bmp";

            AppendColorMappingForWork(colorScheme, workByDeveloper);

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

        public List<object> ExportSummary()
        {
            LoadHistory();
            LoadMetrics();
            LoadContributions(true); // silent

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));
            var hotspotCalculator = new HotspotCalculator(summary, _metrics);

            var orderedByLocalPath = summary.OrderBy(x => x.LocalPath).ToList();
            var gridData = new List<object>();
            foreach (var artifact in orderedByLocalPath)
            {
                gridData.Add(CreateDataGridFriendlyArtifact(artifact, hotspotCalculator));
            }

            var now = DateTime.Now.ToIsoShort();
            
            Csv.Write(Path.Combine(Project.Cache, $"summary-{now}.csv"), gridData);
            return gridData;
        }

        public void UpdateCache(Progress progress, bool includeContributions)
        {
            progress.Message("Updating source control history.");

            // Note: You should have the latest code locally such that history and metrics match!
            // Update svn history
            var svnProvider = Project.CreateProvider();
            svnProvider.UpdateCache(progress);

            progress.Message("Updating code metrics.");

            // Update code metrics
            var metricProvider = new MetricProvider(Project.ProjectBase, Project.Cache, Project.GetNormalizedFileExtensions());
            metricProvider.UpdateCache();

            File.Delete(GetPathToContributionFile());
            if (includeContributions)
            {
                // Update contributions. This takes a long time. Not useful for svn.
                UpdateContributions(progress);
            }
        }

        internal void Clear()
        {
            _history = null;
            _metrics = null;
            _contributions = null;
        }

        internal List<string> GetMainDevelopers()
        {
            LoadContributions();
            return _contributions.Select(x => x.Value.GetMainDeveloper().Developer).Distinct().ToList();
        }

        static string ClassifyDirectory(string localPath)
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

        void AppendColorMappingForWork(ColorScheme colorMapping, Dictionary<string, uint> workByDeveloper)
        {
            // order such that same developers get same colors regardless of order.
            foreach (var developer in workByDeveloper.Keys.OrderBy(x => x))
            {
                colorMapping.AddColorKey(developer);
            }
        }

        Dictionary<string, Contribution> CalculateContributionsParallel(Progress progress, List<Artifact> summary)
        {
            // Calculate main developer for each file
            var fileToContribution = new ConcurrentDictionary<string, Contribution>();

            var all = summary.Count;
            Parallel.ForEach(summary, new ParallelOptions { MaxDegreeOfParallelism = 4 },
                             artifact =>
                             {
                                 var provider = Project.CreateProvider();
                                 var work = provider.CalculateDeveloperWork(artifact);
                                 var contribution = new Contribution(work);

                                 var result = fileToContribution.TryAdd(artifact.LocalPath, contribution);
                                 Debug.Assert(result);

                                 // Progress
                                 var count = fileToContribution.Count;

                                 // if (count % 10 == 0)
                                 {
                                     progress.Message($"Calculating work {count}/{all}");
                                 }
                             });

            return fileToContribution.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
        }

        object CreateDataGridFriendlyArtifact(Artifact artifact, HotspotCalculator hotspotCalculator)
        {
            var linesOfCode = (int) hotspotCalculator.GetArea(artifact);
            if (_contributions != null)
            {
                var result = new DataGridFriendlyArtifact();
                var artifactContribution = _contributions[artifact.LocalPath.ToLowerInvariant()];
                var mainDev = artifactContribution.GetMainDeveloper();

                result.LocalPath = artifact.LocalPath;
                result.Revision = artifact.Revision;
                result.Commits = artifact.Commits;
                result.Committers = artifact.Committers.Count;
                result.LOC = linesOfCode;
                result.WorkItems = artifact.WorkItems.Count;
                result.CodeAge_Days = (DateTime.Now - artifact.Date).Days;
                result.Hotspot = hotspotCalculator.GetHotspot(artifact);

                // Work related information
                result.FractalValue = artifactContribution.CalculateFractalValue();
                result.MainDev = mainDev.Developer;
                result.MainDevPercent = mainDev.Percent;
                return result;
            }
            else
            {
                var result = new DataGridFriendlyArtifactBasic();
                result.LocalPath = artifact.LocalPath;
                result.Revision = artifact.Revision;
                result.Commits = artifact.Commits;
                result.Committers = artifact.Committers.Count;
                result.LOC = linesOfCode;
                result.WorkItems = artifact.WorkItems.Count;
                result.CodeAge_Days = (DateTime.Now - artifact.Date).Days;
                result.Hotspot = hotspotCalculator.GetHotspot(artifact);
                return result;
            }
        }

        string GetPathToContributionFile()
        {
            return Path.Combine(Project.Cache, "contribution_analysis.json");
        }

        void LoadContributions(bool silent = false)
        {
            if (_contributions == null)
            {
                if (File.Exists(GetPathToContributionFile()) == false)
                {
                    if (silent)
                    {
                        return;
                    }

                    throw new Exception($"The file '{GetPathToContributionFile()}' was not found. Please click Sync to create it.");
                }

                var input = File.ReadAllText(GetPathToContributionFile(), Encoding.UTF8);
                _contributions = JsonConvert.DeserializeObject<Dictionary<string, Contribution>>(input);
            }
        }

        void LoadHistory()
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

        void LoadMetrics()
        {
            // Get code metrics (all files from the cache!)
            if (_metrics == null)
            {
                var metricProvider = new MetricProvider(Project.ProjectBase, Project.Cache, Project.GetNormalizedFileExtensions());
                _metrics = metricProvider.QueryCodeMetrics();
            }
        }

        void UpdateContributions(Progress progress)
        {
            LoadHistory();
            LoadMetrics();

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));
            _contributions = CalculateContributionsParallel(progress, summary);

            var json = JsonConvert.SerializeObject(_contributions);
            var path = GetPathToContributionFile();
            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }
}