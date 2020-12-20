using Insight.Analyzers;
using Insight.Builder;
using Insight.Dto;
using Insight.Metrics;
using Insight.Shared;
using Insight.Shared.Extensions;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Insight.Alias;

using Visualization.Controls;
using Visualization.Controls.Bitmap;
using Visualization.Controls.Interfaces;

namespace Insight
{
    public sealed class Analyzer
    {
        private readonly IMetricProvider _metricsProvider;
        private readonly string[] _supportedFileTypesForAnalysis;

        /// <summary>
        /// Local path -> contribution
        /// </summary>
        private Dictionary<string, Contribution> _contributions;

        private ChangeSetHistory _history;
        private Dictionary<string, LinesOfCode> _metrics;
        private IFilter _extendedDisplayFilter;
        private ISourceControlProvider _sourceProvider;
        private string _outputPath;


        public Analyzer(IMetricProvider metricProvider, string[] supportedFileTypesForAnalysis)
        {
            _metricsProvider = metricProvider;
            _supportedFileTypesForAnalysis = supportedFileTypesForAnalysis;
        }

        /// <summary>
        /// Set all properties that are associated with a selected project.
        /// Call this whenever you load a new project or change an existing project.
        /// </summary>
        public void Configure(ISourceControlProvider provider, string outputPath, IFilter displayFilter)
        {
            Clear();
            _sourceProvider = provider;
            _outputPath = outputPath;
            _extendedDisplayFilter = displayFilter;

            LoadCachedData();

            // Needs metrics
            _extendedDisplayFilter = CreateExtendedFilter(displayFilter);
        }

        public List<WarningMessage> Warnings { get; private set; }

        public List<Coupling> AnalyzeChangeCoupling()
        {
            // Pair wise couplings
            var couplingAnalyzer = new ChangeCouplingAnalyzer();
            var couplings = couplingAnalyzer.CalculateChangeCouplings(_history, _extendedDisplayFilter);
            var sortedCouplings = couplings.OrderByDescending(coupling => coupling.Degree).ToList();
            Csv.Write(Path.Combine(_outputPath, "change_couplings.csv"), sortedCouplings);


            // TODO I removed this from the wiki
            // Same with classified folders (show this one if available)
            var mappingFile = Path.Combine(_outputPath, "logical_components.xml");
            if (File.Exists(mappingFile))
            {
                return AnalyzeLogicalComponentChangeCoupling(mappingFile);
            }

            return sortedCouplings;
        }

        /// <summary>
        /// Extends the display filter from project to accept only files from the metrics list.
        /// </summary>
        private IFilter CreateExtendedFilter(IFilter displayFilter)
        {
            LoadMetrics();

            // This is way faster than File.Exists. I already know these files exist because I calculated a metric
            var localFiles = new HashSet<string>(_metrics.Keys);

            var onlyMetricFiles = new FileFilter(localFiles);
            return new Filter(displayFilter, onlyMetricFiles);
        }


        public HierarchicalDataContext AnalyzeCodeAge()
        {
            // Get summary of all files
            var summary = _history.GetArtifactSummary(_extendedDisplayFilter, new NullAliasMapping());

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
        public HierarchicalDataContext AnalyzeFragmentation(IAliasMapping aliasMapping)
        {
            LoadContributions(false);
            var localFileToContribution = AliasTransformContribution(_contributions, aliasMapping);

            var summary = _history.GetArtifactSummary(_extendedDisplayFilter, aliasMapping);
            var fileToFractalValue = localFileToContribution.ToDictionary(pair => pair.Key, pair => pair.Value.CalculateFractalValue());

            var builder = new FragmentationBuilder();
            var hierarchicalData = builder.Build(summary, _metrics, fileToFractalValue);

            var dataContext = new HierarchicalDataContext(hierarchicalData);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.Fragmentation;
            return dataContext;
        }

        public HierarchicalDataContext AnalyzeHotspots()
        {
            // Get summary of all files
            var summary = _history.GetArtifactSummary(_extendedDisplayFilter, new NullAliasMapping());

            var builder = new HotspotBuilder();
            var hierarchicalData = builder.Build(summary, _metrics);
            var dataContext = new HierarchicalDataContext(hierarchicalData);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.NumberOfCommits;
            return dataContext;
        }

        /// <summary>
        /// The contributions are calculated by the source control providers.
        /// In order to use developer aliases we have to transform the contributions, too.
        /// If two developers are mapped to the same alias their contribution is shared.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Contribution> AliasTransformContribution(Dictionary<string, Contribution> localFilesToContribution, IAliasMapping aliasMapping)
        {
            var localFileToAliasContribution = new Dictionary<string, Contribution>();

            foreach (var fileToContribution in localFilesToContribution)
            {
                var localFile = fileToContribution.Key;
                var developerToWork = fileToContribution.Value.DeveloperToContribution;
                var aliasToWork = AliasTransformWork(aliasMapping, developerToWork);
                localFileToAliasContribution.Add(localFile, new Contribution(aliasToWork));
            }
                
            // local file -> contribution
            return localFileToAliasContribution;
        }

        private static Dictionary<string, uint> AliasTransformWork(IAliasMapping aliasMapping, Dictionary<string, uint> developerToWork)
        {
            var aliasToWork = new Dictionary<string, uint>();

            // Group by alias
            var groups = developerToWork.GroupBy(pair => aliasMapping.GetAlias(pair.Key));
            foreach (var group in groups)
            {
                var sumContribution = (uint) group.Sum(g => g.Value);
                var alias = group.Key;
                aliasToWork.Add(alias, sumContribution);
            }

            return aliasToWork;
        }

        public string AnalyzeWorkOnSingleFile(string fileName, IBrushFactory brushFactory, IAliasMapping aliasMapping)
        {
            var workByDeveloper = _sourceProvider.CalculateDeveloperWork(fileName);

            var workByAlias = AliasTransformWork(aliasMapping, workByDeveloper);

            var bitmap = new FractionBitmap();

            var fi = new FileInfo(fileName);
            var path = Path.Combine(_outputPath, fi.Name) + ".bmp";


            // TODO atr bitmap?
            bitmap.Create(path, workByAlias, brushFactory, true);

            return path;
        }

        public HierarchicalDataContext AnalyzeKnowledge(IBrushFactory brushFactory, IAliasMapping aliasMapping)
        {
            LoadContributions(false);
            var localFileToContribution = AliasTransformContribution(_contributions, aliasMapping);

            var summary = _history.GetArtifactSummary(_extendedDisplayFilter, aliasMapping);
            var fileToMainDeveloper = localFileToContribution.ToDictionary(pair => pair.Key, pair => pair.Value.GetMainDeveloper());

            // Assign a color to each developer
            var mainDevelopers = fileToMainDeveloper.Select(pair => pair.Value.Developer).Distinct().ToList();

            var legend = new LegendBitmap(mainDevelopers, brushFactory);
            legend.CreateLegendBitmap(Path.Combine(_outputPath, "knowledge_color.bmp"));

            // Build the knowledge data
            var builder = new KnowledgeBuilder();
            var hierarchicalData = builder.Build(summary, _metrics, fileToMainDeveloper);

            var dataContext = new HierarchicalDataContext(hierarchicalData, brushFactory);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.NotAvailable;
            return dataContext;
        }


        /// <summary>
        /// Same as knowledge but uses a different color scheme
        /// </summary>
        public HierarchicalDataContext AnalyzeKnowledgeLoss(string developer, IBrushFactory brushFactory, IAliasMapping aliasMapping)
        {
            LoadContributions(false);
            var localFileToContribution = AliasTransformContribution(_contributions, aliasMapping);

            developer = aliasMapping.GetAlias(developer);

            var summary = _history.GetArtifactSummary(_extendedDisplayFilter, aliasMapping);
            var fileToMainDeveloper = localFileToContribution.ToDictionary(pair => pair.Key, pair => pair.Value.GetMainDeveloper());

            // Build the knowledge data
            var builder = new KnowledgeBuilder(developer);
            var hierarchicalData = builder.Build(summary, _metrics, fileToMainDeveloper);

            var dataContext = new HierarchicalDataContext(hierarchicalData, brushFactory);
            dataContext.AreaSemantic = Strings.LinesOfCode;
            dataContext.WeightSemantic = Strings.NotAvailable;
            return dataContext;
        }

        public List<TrendData> AnalyzeTrend(string localFile)
        {
            var trend = new List<TrendData>();

            // Log on this file to get all revisions
            var fileHistory = _sourceProvider.ExportFileHistory(localFile);

            // For each file we need to calculate the metrics LOC and inverted whitespace.
            foreach (var file in fileHistory)
            {
                var fileInfo = new FileInfo(file.CachePath);
                var loc = _metricsProvider.CalculateLinesOfCode(fileInfo);
                var invertedSpace = _metricsProvider.CalculateInvertedSpaceMetric(fileInfo);
                trend.Add(new TrendData { Date = file.Date, Loc = loc, InvertedSpace = invertedSpace });
            }

            return trend;
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

            Csv.Write(Path.Combine(_outputPath, "comments.csv"), result);
            return result;
        }

        public List<object> ExportSummary(IAliasMapping aliasMapping)
        {
            LoadContributions(true); // silent

            var summary = _history.GetArtifactSummary(_extendedDisplayFilter, aliasMapping);
            var hotspotCalculator = new HotspotCalculator(summary, _metrics);

            var orderedByLocalPath = summary.OrderBy(x => x.LocalPath).ToList();
            var gridData = new List<object>();
            foreach (var artifact in orderedByLocalPath)
            {
                gridData.Add(CreateDataGridFriendlyArtifact(artifact, hotspotCalculator, aliasMapping));
            }

            var now = DateTime.Now.ToIsoShort();

            Csv.Write(Path.Combine(_outputPath, $"summary-{now}.csv"), gridData);
            return gridData;
        }

        
        public void UpdateCache(Progress progress, bool includeContributions)
        {
            Clear();
            
            progress.Message("Updating source control history.");

            var supportedFilesFilter = new ExtensionIncludeFilter(_supportedFileTypesForAnalysis);

            // Note: You should have the latest code locally such that history and metrics match!
            _sourceProvider.UpdateCache(progress, includeContributions, supportedFilesFilter);

            progress.Message("Updating code metrics.");

            // Update code metrics
            _metricsProvider.UpdateLinesOfCodeCache(_sourceProvider.BaseDirectory, _outputPath, _supportedFileTypesForAnalysis);

            Warnings = _sourceProvider.Warnings;

            LoadCachedData();
        }

        private void LoadCachedData()
        {
            LoadHistory();
            LoadMetrics();
        }

        internal void Clear()
        {
            _history = null;
            _metrics = null;
            _contributions = null;
        }

        internal List<string> GetMainDevelopers(IAliasMapping aliasMapping)
        {
            LoadContributions(false);
            var localFileToContribution = AliasTransformContribution(_contributions, aliasMapping);
            var mainDevelopers = localFileToContribution.Select(x => x.Value.GetMainDeveloper().Developer).Distinct();
            return mainDevelopers.ToList();
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


        private List<Coupling> AnalyzeLogicalComponentChangeCoupling(string mappingFile)
        {
            var couplingAnalyzer = new ChangeCouplingAnalyzer();
            var mapper = new LogicalComponentMapper();
            mapper.ReadDefinitionFile(mappingFile, true);

            Func<string, string> classifier = localPath => { return mapper.MapLocalPathToLogicalComponent(localPath); };

            var classifiedCouplings = couplingAnalyzer.CalculateClassifiedChangeCouplings(_history, classifier);
            Csv.Write(Path.Combine(_outputPath, "classified_change_couplings.csv"), classifiedCouplings);

            return classifiedCouplings;
        }

        /// <summary>
        /// Returns developers ordered by the number of commits.
        /// </summary>
        public List<string> GetAllKnownDevelopers()
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


        private object CreateDataGridFriendlyArtifact(Artifact artifact, HotspotCalculator hotspotCalculator, IAliasMapping aliasMapping)
        {
            var linesOfCode = (int) hotspotCalculator.GetLinesOfCode(artifact);
            if (_contributions != null)
            {
                var result = new DataGridFriendlyArtifact();

                var localFileToContribution = AliasTransformContribution(_contributions, aliasMapping);
                var contribution = localFileToContribution[artifact.LocalPath.ToLowerInvariant()];
                var mainDev = contribution.GetMainDeveloper();

                result.LocalPath = artifact.LocalPath;
                result.Commits = artifact.Commits;
                result.Committers = artifact.Committers.Count;
                result.LOC = linesOfCode;
                result.WorkItems = artifact.WorkItems.Count;
                result.CodeAge_Days = (DateTime.Now - artifact.Date).Days;
                result.Hotspot = hotspotCalculator.GetHotspotValue(artifact);

                // Work related information
                result.FractalValue = contribution.CalculateFractalValue();
                result.MainDev = mainDev.Developer;
                result.MainDevPercent = mainDev.Percent;
                return result;
            }
            else
            {
                var result = new DataGridFriendlyArtifactBasic();
                result.LocalPath = artifact.LocalPath;
                result.Commits = artifact.Commits;
                result.Committers = artifact.Committers.Count;
                result.LOC = linesOfCode;
                result.WorkItems = artifact.WorkItems.Count;
                result.CodeAge_Days = (DateTime.Now - artifact.Date).Days;
                result.Hotspot = hotspotCalculator.GetHotspotValue(artifact);
                return result;
            }
        }

        private void LoadContributions(bool silent)
        {
            if (_contributions == null)
            {
                _contributions = _sourceProvider.QueryContribution();

                if (_contributions == null && !silent)
                {
                    throw new FileNotFoundException("Contribution data is not available. Sync to create it.");
                }
            }
        }

        private void LoadHistory()
        {
            if (_history == null)
            {
                // Assume the history was already cleaned such that it only contains tracked files and no deletes.
                _history = _sourceProvider.QueryChangeSetHistory();
            }
        }

        private void LoadMetrics()
        {
            // Get code metrics (all files from the cache!)
            if (_metricsProvider != null)
            {
                _metrics = _metricsProvider.QueryLinesOfCode(_outputPath);
            }
        }
    }
}