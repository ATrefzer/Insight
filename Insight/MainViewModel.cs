using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;

using Insight.Analyzers;
using Insight.Shared;
using Insight.Shared.Model;
using Insight.ViewModels;
using Insight.WpfCore;

using Prism.Commands;

using Visualization.Controls;
using Visualization.Controls.Data;
using Visualization.Controls.Interfaces;
using Visualization.Controls.Tools;

namespace Insight
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly Analyzer _analyzer;
        private readonly BackgroundExecution _backgroundExecution;
        private readonly DialogService _dialogs;
        private readonly Project _project;
        private readonly TabBuilder _tabBuilder;
        private readonly ViewController _viewController;

        private int _selectedIndex = -1;
        private ObservableCollection<TabContentViewModel> _tabs = new ObservableCollection<TabContentViewModel>();

        public MainViewModel(ViewController viewController, DialogService dialogs, Project project, Analyzer analyzer, BackgroundExecution backgroundExecution)
        {
            _tabBuilder = new TabBuilder(this);
            _viewController = viewController;
            _dialogs = dialogs;
            _project = project;
            _analyzer = analyzer;
            _backgroundExecution = backgroundExecution;

            SetupCommand = new DelegateCommand(SetupClick);
            UpdateCommand = new DelegateCommand(UpdateClick);
            FragmentationCommand = new DelegateCommand(FragmentationClick);
            LoadDataCommand = new DelegateCommand(LoadDataClick);
            SaveDataCommand = new DelegateCommand(SaveDataClick);
            SummaryCommand = new DelegateCommand(SummaryClick);
            CommentsCommand = new DelegateCommand(CommentsClick);
            KnowledgeCommand = new DelegateCommand(KnowledgeClick);
            KnowledgeLossCommand = new DelegateCommand(KnowledgeLossClick);
            HotspotsCommand = new DelegateCommand(HotspotsClick);
            CodeAgeCommand = new DelegateCommand(CodeAgeClick);
            ChangeCouplingCommand = new DelegateCommand(ChangeCouplingClick);
            AboutCommand = new DelegateCommand(AboutClick);
            PredictHotspotsCommand = new DelegateCommand(PredictHotspotsClick);
        }

        public ICommand AboutCommand { get; set; }

        public ICommand ChangeCouplingCommand { get; set; }


        public ICommand CodeAgeCommand { get; set; }

        public ICommand CommentsCommand { get; set; }

        public ICommand FragmentationCommand { get; set; }

        public ICommand HotspotsCommand { get; set; }

        public bool IsProjectValid => _project.IsValid();

        public ICommand KnowledgeCommand { get; set; }

        public ICommand KnowledgeLossCommand { get; set; }

        public ICommand LoadDataCommand { get; set; }

        public ICommand PredictHotspotsCommand { get; set; }

        public ICommand SaveDataCommand { get; set; }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                ToolsExtension.Instance.CloseToolWindow();
                _selectedIndex = value;
                OnPropertyChanged();
            }
        }

        public ICommand SetupCommand { get; set; }

        public ICommand SummaryCommand { get; set; }

        public ObservableCollection<TabContentViewModel> Tabs
        {
            get => _tabs;
            set
            {
                _tabs = value;
                OnPropertyChanged();
            }
        }

        public ICommand UpdateCommand { get; set; }

        public void OnShowChangeCouplingChord(List<Coupling> args)
        {
            if (args.Any())
            {
                var edges = args.Select(coupling => CreateEdgeData(coupling));

                _tabBuilder.ShowChangeCoupling(edges.ToList());
            }
        }

        public async void OnShowTrend(IHierarchicalData data)
        {
            var localFile = data.Tag as string;
            Debug.Assert(!string.IsNullOrEmpty(localFile));

            var trendData = await _backgroundExecution.ExecuteAsync(() => _analyzer.AnalyzeTrend(localFile));
            if (trendData == null)
            {
                // Exception was handled but there is not data.
                return;
            }

            var ordered = trendData.OrderBy(x => x.Date).ToList();
            _viewController.ShowTrendViewer(ordered);
        }


        public async void OnShowWork(IHierarchicalData data)
        {
            var fileToAnalyze = data.Tag as string;
            var path = await _backgroundExecution.ExecuteAsync(() => _analyzer.AnalyzeWorkOnSingleFile(fileToAnalyze)).ConfigureAwait(true);

            if (path == null)
            {
                return;
            }

            _viewController.ShowImageViewer(path);
        }

        public void Refresh()
        {
            OnAllPropertyChanged();
        }


        private void AboutClick()
        {
            _viewController.ShowAbout();
        }

        private async void ChangeCouplingClick()
        {
            var couplings = await _backgroundExecution.ExecuteAsync(_analyzer.AnalyzeChangeCoupling);
            _tabBuilder.ShowChangeCoupling(couplings);
        }

        private async void CodeAgeClick()
        {
            // Analyze hotspots from summary and code metrics
            var context = await _backgroundExecution.ExecuteAsync(_analyzer.AnalyzeCodeAge);
            var colorScheme = context.ColorScheme;

            _tabBuilder.ShowHierarchicalDataAsCirclePackaging("Code Age", context, GetDefaultCommands());
            _tabBuilder.ShowHierarchicalDataAsTreeMap("Code Age", context.Clone(), GetDefaultCommands());
            _tabBuilder.ShowWarnings(_analyzer.Warnings);
        }

        private async void CommentsClick()
        {
            var comments = await _backgroundExecution.ExecuteAsync(_analyzer.ExportComments);
            _tabBuilder.ShowText(comments, "Comments");
        }

        private EdgeData CreateEdgeData(Coupling coupling)
        {
            return new EdgeData(coupling.Item1, coupling.Item2, coupling.Degree)
                   {
                           Node1DisplayName = GetVertexName(coupling.Item1),
                           Node2DisplayName = GetVertexName(coupling.Item2)
                   };
        }

        private async void FragmentationClick()
        {
            var context = await _backgroundExecution.ExecuteAsync(() => _analyzer.AnalyzeFragmentation());
            if (context == null)
            {
                return;
            }

            var colorScheme = context.ColorScheme;

            _tabBuilder.ShowHierarchicalDataAsCirclePackaging("Fragmentation", context, GetDefaultCommands());
            _tabBuilder.ShowHierarchicalDataAsTreeMap("Fragmentation", context.Clone(), GetDefaultCommands());

            //_tabBuilder.ShowImage(new BitmapImage(new Uri(path)));
        }

        private HierarchicalDataCommands GetDefaultCommands()
        {
            var commands = new HierarchicalDataCommands();
            commands.Register("Trend", OnShowTrend);
            commands.Register("Work", data => OnShowWork(data));

            return commands;
        }

        private string GetDeveloperForKnowledgeLoss()
        {
            string forDeveloper;
            try
            {
                var mainDevelopers = _analyzer.GetMainDevelopers();
                forDeveloper = _viewController.SelectDeveloper(mainDevelopers);
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(ex.Message);
                forDeveloper = null;
            }

            return forDeveloper;
        }

        private string GetVertexName(string path)
        {
            var lastBackSlash = path.LastIndexOf('\\');
            var lastSlash = path.LastIndexOf('/');

            var index = Math.Max(lastBackSlash, lastSlash);
            if (index < 0)
            {
                return path;
            }

            return path.Substring(index + 1);
        }

        private async void HotspotsClick()
        {
            // Analyze hotspots from summary and code metrics
            var context = await _backgroundExecution.ExecuteAsync(_analyzer.AnalyzeHotspots);
            if (context == null)
            {
                return;
            }

            var colorScheme = context.ColorScheme;

            _tabBuilder.ShowHierarchicalDataAsCirclePackaging("Hotspots", context, GetDefaultCommands());
            _tabBuilder.ShowHierarchicalDataAsTreeMap("Hotspots", context.Clone(), GetDefaultCommands());
            _tabBuilder.ShowWarnings(_analyzer.Warnings);
        }

        private async void KnowledgeClick()
        {
            var context = await _backgroundExecution.ExecuteAsync(() => _analyzer.AnalyzeKnowledge());
            if (context == null)
            {
                return;
            }

            var colorScheme = context.ColorScheme;

            _tabBuilder.ShowHierarchicalDataAsCirclePackaging("Knowledge", context, GetDefaultCommands());
            _tabBuilder.ShowHierarchicalDataAsTreeMap("Knowledge", context.Clone(), GetDefaultCommands());
        }

        private async void KnowledgeLossClick()
        {
            var forDeveloper = GetDeveloperForKnowledgeLoss();
            if (forDeveloper == null)
            {
                return;
            }

            var context = await _backgroundExecution.ExecuteAsync(() => _analyzer.AnalyzeKnowledgeLoss(forDeveloper));
            if (context == null)
            {
                return;
            }

            var colorScheme = context.ColorScheme;

            _tabBuilder.ShowHierarchicalDataAsCirclePackaging($"Loss {forDeveloper}", context, GetDefaultCommands());
            _tabBuilder.ShowHierarchicalDataAsTreeMap($"Loss {forDeveloper}", context.Clone(), GetDefaultCommands());
        }

        private void PredictHotspotsClick()
        {
            var oldFile = _dialogs.GetLoadFile("csv", "Load old summary", _project.Cache);
            if (oldFile == null)
            {
                return;
            }
            var newFile = _dialogs.GetLoadFile("csv", "Load current summary", _project.Cache);
            if (newFile == null)
            {
                return;
            }

            try
            {
                var predictor = new HotspotPredictor(oldFile, newFile);
                var deltas = predictor.GetHotspotDelta().OrderByDescending(x => x.Delta);
                _tabBuilder.ShowText(deltas, "Future Hotspots");
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(ex.Message);
            }
        }

        private void Save(string fileName, IHierarchicalData data)
        {
            var file = new BinaryFile<IHierarchicalData>();
            file.Write(fileName, data);
        }


        string MakeColorsFile(string fileName)
        {
            return fileName + ".colors";
        }

        private void SaveDataClick()
        {         
            if (Tabs.Any() == false || SelectedIndex < 0)
            {
                return;
            }

            var descr = Tabs.ElementAt(SelectedIndex);

            // Saving hierarchical data
            if (descr.Data is HierarchicalDataContext context)
            {
                var fileName = _dialogs.GetSaveFile("bin", "Save data", _project.Cache);
                if (fileName != null)
                {
                    Save(fileName, context.Data);

                    var colorScheme = context.ColorScheme as ColorScheme;
                    if (colorScheme != null)
                    {
                        var json = new JsonFile<ColorScheme>();
                        json.Write(MakeColorsFile(fileName), colorScheme);
                    }
                }
            }
        }


        private void LoadDataClick()
        {
            try
            {
                var fileName = _dialogs.GetLoadFile("bin", "Load data", _project.Cache);
                if (fileName != null)
                {
                    // Read hierarchical data
                    var file = new BinaryFile<HierarchicalData>();
                    var data = file.Read(fileName);

                    // Read coloring
                    var colorScheme = new ColorScheme();
                    var colorFile = MakeColorsFile(fileName);
                    if (File.Exists(colorFile))
                    {
                        var json = new JsonFile<ColorScheme>();
                        colorScheme = json.Read(colorFile);
                    }

                    var context = new HierarchicalDataContext(data, colorScheme);
                    _tabBuilder.ShowHierarchicalDataAsCirclePackaging("Loaded", context, null);
                    _tabBuilder.ShowHierarchicalDataAsTreeMap("Loaded", context.Clone(), null);
                }
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(ex.Message);
            }
        }


        private void SetupClick()
        {
            var changed = _viewController.ShowProjectSettings(_project);
            if (changed)
            {
                _tabs.Clear();
                _analyzer.Clear();
            }

            // Refresh state of ribbon
            Refresh();
        }

        private async void SummaryClick()
        {
            var summary = await _backgroundExecution.ExecuteAsync(_analyzer.ExportSummary);

            _tabBuilder.ShowText(summary, "Summary");
        }

        private async void UpdateClick()
        {
            // The functions to update or pull are implemented in SvnProvider and GitProvider.
            // But actually that is not the task of this tool. Give it an updated repository.

            if (!_dialogs.AskYesNoQuestion(Strings.SyncInstructions, Strings.Confirm))
            {
                return;
            }

            // Contributions may be too much if using svn.
            var includeContributions = _dialogs.AskYesNoQuestion(Strings.SyncIncludeContributions, Strings.Confirm);

            await _backgroundExecution.ExecuteWithProgressAsync(progress => _analyzer.UpdateCache(progress, includeContributions));
           
            _tabs.Clear();
        }
    }
}