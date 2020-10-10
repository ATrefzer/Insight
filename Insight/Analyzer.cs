using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Insight.Analyzers;
using Insight.Builder;
using Insight.Dto;
using Insight.Metrics;
using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;
using Visualization.Controls;
using Visualization.Controls.Bitmap;
using Visualization.Controls.Interfaces;

namespace Insight
{
    public sealed class Analyzer
    {
        /// <summary>
        /// Local path -> contribution
        /// </summary>
        Dictionary<string, Contribution> _contributions;

        ChangeSetHistory _history;
        Dictionary<string, LinesOfCode> _metrics;
        private readonly IMetricProvider _metricsProvider;

        // TODO atr Remove the color schema manager here.
        // TODO atr Analyzer should return only data. Let the main view model manage this.

        private ColorSchemeManager _colorSchemeManager;

        public Analyzer(IMetricProvider metricProvider)
        {
            _metricsProvider = metricProvider;
        }

        public List<WarningMessage> Warnings { get; private set; }

        
        // TODO atr remove this property and pass required information to method calls
        // We only need filter, cache directory and source code provider
        private IProject _project;
        public IProject Project
        {
            get
            {
                return _project;
            }
            set
            {
                _project = value;
                _colorSchemeManager = new ColorSchemeManager(Project.Cache);
            }
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

        public List<Coupling> AnalyzeChangeCoupling()
        {
            LoadHistory();

            // Pair wise couplings
            var couplingAnalyzer = new ChangeCouplingAnalyzer();
            var couplings = couplingAnalyzer.CalculateChangeCouplings(_history, Project.Filter);
            var sortedCouplings = couplings.OrderByDescending(coupling => coupling.Degree).ToList();
            Csv.Write(Path.Combine(Project.Cache, "change_couplings.csv"), sortedCouplings);

            // Same with classified folders (show this one if available)
            var mappingFile = Path.Combine(Project.Cache, "logical_components.xml");
            if (File.Exists(mappingFile))
            {
                return AnalyzeLogicalComponentChangeCoupling(mappingFile);
            }

            return sortedCouplings;
        }


       
        private List<Coupling> AnalyzeLogicalComponentChangeCoupling(string mappingFile)
        {
            var couplingAnalyzer = new ChangeCouplingAnalyzer();
            var mapper = new LogicalComponentMapper();
            mapper.ReadDefinitionFile(mappingFile, true);

            Func<string, string> classifier = (localPath) =>
            {
                return mapper.MapLocalPathToLogicalComponent(localPath);
            };

            var classifiedCouplings = couplingAnalyzer.CalculateClassifiedChangeCouplings(_history, classifier);
            Csv.Write(Path.Combine(Project.Cache, "classified_change_couplings.csv"), classifiedCouplings);

            return classifiedCouplings;
        }

        public HierarchicalDataContext AnalyzeCodeAge()
        {
            LoadHistory();
            LoadMetrics();
          
            // Get summary of all files
            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));

            var builder = new CodeAgeBuilder();
            var hierarchicalData = builder.Build(summary, _metrics);
            var dataContext = new HierarchicalDataContext(hierarchicalData);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.CodeAge_Days;
            return dataContext;
        }

        /// <summary>
        /// Analyzes the fragmentation per file.
        /// </summary>
        public HierarchicalDataContext AnalyzeFragmentation()
        {
            LoadHistory();
            LoadMetrics();
            LoadContributions(false);

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));
            var fileToFractalValue = _contributions.ToDictionary(pair => pair.Key, pair => pair.Value.CalculateFractalValue());

            var builder = new FragmentationBuilder();
            var hierarchicalData = builder.Build(summary, _metrics, fileToFractalValue);

            var dataContext = new HierarchicalDataContext(hierarchicalData);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.Fragmentation;
            return dataContext;
        }

        public HierarchicalDataContext AnalyzeHotspots()
        {
            LoadHistory();
            LoadMetrics();

            // Get summary of all files
            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));

            var builder = new HotspotBuilder();
            var hierarchicalData = builder.Build(summary, _metrics);
            var dataContext = new HierarchicalDataContext(hierarchicalData);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.NumberOfCommits;
            return dataContext;
        }

        public HierarchicalDataContext AnalyzeKnowledge()
        {
            LoadHistory();
            LoadMetrics();
            LoadContributions(false);
            LoadColorScheme();

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));
            var fileToMainDeveloper = _contributions.ToDictionary(pair => pair.Key, pair => pair.Value.GetMainDeveloper());

            // Assign a color to each developer
            var mainDevelopers = fileToMainDeveloper.Select(pair => pair.Value.Developer).Distinct().ToList();

            var legend = new LegendBitmap(mainDevelopers, _colorScheme);
            legend.CreateLegendBitmap(Path.Combine(Project.Cache, "knowledge_color.bmp"));

            // Build the knowledge data
            var builder = new KnowledgeBuilder();
            var hierarchicalData = builder.Build(summary, _metrics, fileToMainDeveloper);

            // TODO atr This should be moved to the view model.
            var dataContext = new HierarchicalDataContext(hierarchicalData, _colorScheme);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.NotAvailable;
            return dataContext;
        }

        // TODO atr move to view model (Project)
        IColorScheme _colorScheme;

        void LoadColorScheme()
        {
            if (_colorScheme != null)
            {
                return;
            }
 
            _colorScheme = _colorSchemeManager.GetColorScheme() ?? new ColorScheme();
           
        }


        /// <summary>
        /// Same as knowledge but uses a different color scheme
        /// </summary>
        public HierarchicalDataContext AnalyzeKnowledgeLoss(string developer)
        {
            LoadHistory();
            LoadMetrics();
            LoadContributions(false);
            LoadColorScheme();

            var summary = _history.GetArtifactSummary(Project.Filter, new HashSet<string>(_metrics.Keys));
            var fileToMainDeveloper = _contributions.ToDictionary(pair => pair.Key, pair => pair.Value.GetMainDeveloper());

            // Build the knowledge data
            var builder = new KnowledgeBuilder(developer);
            var hierarchicalData = builder.Build(summary, _metrics, fileToMainDeveloper);

            var dataContext = new HierarchicalDataContext(hierarchicalData, _colorScheme);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.NotAvailable;
            return dataContext;
        }

        public List<TrendData> AnalyzeTrend(string localFile)
        {
            var trend = new List<TrendData>();

            var provider = Project.CreateProvider();

            // Log on this file to get all revisions
            var fileHistory = provider.ExportFileHistory(localFile);

            // For each file we need to calculate the metrics

            foreach (var file in fileHistory)
            {
                var fileInfo = new FileInfo(file.CachePath);
                var loc = _metricsProvider.CalculateLinesOfCode(fileInfo);
                var invertedSpace = _metricsProvider.CalculateInvertedSpaceMetric(fileInfo);
                trend.Add(new TrendData { Date = file.Date, Loc = loc, InvertedSpace = invertedSpace });
            }

            return trend;
        }
       
        public string AnalyzeWorkOnSingleFile(string fileName)
        {
            LoadColorScheme();

            var provider = Project.CreateProvider();
            var workByDeveloper = provider.CalculateDeveloperWork(fileName);

            var bitmap = new FractionBitmap();

            var fi = new FileInfo(fileName);
            var path = Path.Combine(Project.Cache, fi.Name) + ".bmp";

            var orderedNames = workByDeveloper.Keys.OrderByDescending(name => workByDeveloper[name]).ToList();

            if (_colorSchemeManager.UpdateColorScheme(orderedNames))
            {
                // This may happen if we do not have the latest sources and a new developer occurs.
                _colorScheme = null;
                LoadColorScheme();
            }

            // TODO atr bitmap?
            bitmap.Create(path, workByDeveloper, _colorScheme, true);

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
            Clear();

            progress.Message("Updating source control history.");

            // Note: You should have the latest code locally such that history and metrics match!
            // Update svn history
            var provider = Project.CreateProvider();
            provider.UpdateCache(progress, includeContributions);

            progress.Message("Updating code metrics.");

            // Update code metrics
            _metricsProvider.UpdateLinesOfCodeCache(Project.SourceControlDirectory, Project.Cache, Project.GetNormalizedFileExtensions());

            // Don't delete, only extend if necessary
            _colorSchemeManager.UpdateColorScheme(GetAllKnownDevelopers());
        }

        /// <summary>
        /// Returns developers ordered by the number of commits.
        /// </summary>
        private List<string> GetAllKnownDevelopers()
        {
            LoadHistory();

            // All currently known developers extracted from history
            var dict = new Dictionary<string, uint>();
            foreach (var cs in _history.ChangeSets)
            {
                dict.AddToValue(cs.Committer, 1);
            }
            var developersOrderedByCommits = dict.Keys.OrderBy(key => dict[key]).ToList();
            return developersOrderedByCommits;
        }


        internal void Clear()
        {
            _history = null;
            _metrics = null;
            _contributions = null;
            _colorScheme = null;
        }

        internal List<string> GetMainDevelopers()
        {
            LoadContributions(false);
            return _contributions.Select(x => x.Value.GetMainDeveloper().Developer).Distinct().ToList();
        }


        object CreateDataGridFriendlyArtifact(Artifact artifact, HotspotCalculator hotspotCalculator)
        {
            var linesOfCode = (int)hotspotCalculator.GetLinesOfCode(artifact);
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
                result.Hotspot = hotspotCalculator.GetHotspotValue(artifact);

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
                result.Hotspot = hotspotCalculator.GetHotspotValue(artifact);
                return result;
            }
        }

        void LoadContributions(bool silent)
        {
            if (_contributions == null)
            {
                var provider = Project.CreateProvider();
                _contributions = provider.QueryContribution();

                if (_contributions == null && !silent)
                {
                    throw new FileNotFoundException("Contribution data is not available. Sync to create it.");
                }
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
            if (_metricsProvider != null)
            {
                _metrics = _metricsProvider.QueryCachedLinesOfCode(Project.Cache);
            }
        }
    }
}