using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Insight.Dto;
using Insight.Shared;
using Insight.Shared.Model;
using Insight.ViewModels;
using Insight.WpfCore;

using Prism.Commands;

using Visualization.Controls;
using Visualization.Controls.Data;

namespace Insight
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly Analyzer _analyzer;
        private readonly BackgroundExecution _backgroundExecution;
        private readonly Dialogs _dialogs;
        private readonly Project _project;
        private readonly TabBuilder _tabBuilder;
        private readonly ViewController _viewController;

        private int _selectedIndex = -1;
        private ObservableCollection<TabContentViewModel> _tabs = new ObservableCollection<TabContentViewModel>();

        public MainViewModel(ViewController viewController, Dialogs dialogs, Project project, Analyzer analyzer, BackgroundExecution backgroundExecution)
        {
            _tabBuilder = new TabBuilder(this);
            _viewController = viewController;
            _dialogs = dialogs;
            _project = project;
            _analyzer = analyzer;
            _backgroundExecution = backgroundExecution;

            SetupCommand = new DelegateCommand(SetupClick);
            UpdateCommand = new DelegateCommand(UpdateClick);
            WorkOnSingleFileCommand = new DelegateCommand(WorkOnSingleFileClick);
            LoadDataCommand = new DelegateCommand(LoadDataClick);
            SaveDataCommand = new DelegateCommand(SaveDataClick);
            SummaryCommand = new DelegateCommand(SummaryClick);
            CommentsCommand = new DelegateCommand(CommentsClick);
            KnowledgeCommand = new DelegateCommand(KnowledgeClick);
            HotspotsCommand = new DelegateCommand(HotspotsClick);
            ChangeCouplingCommand = new DelegateCommand(ChangeCouplingClick);
            AboutCommand = new DelegateCommand(AboutClick);
        }

        public ICommand AboutCommand { get; set; }

        public ICommand ChangeCouplingCommand { get; set; }

        public ICommand CommentsCommand { get; set; }

        public ICommand HotspotsCommand { get; set; }

        public bool IsProjectValid => _project.IsValid();

        public ICommand KnowledgeCommand { get; set; }

        public ICommand LoadDataCommand { get; set; }

        public ICommand SaveDataCommand { get; set; }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
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

        public ICommand WorkOnSingleFileCommand { get; set; }

        public void OnShowChangeCouplingChord(List<Coupling> args)
        {
            if (args.Any())
            {
                var edges = args.Select(coupling => CreateEdgeData(coupling));

                _tabBuilder.ShowChangeCoupling(edges.ToList());
            }
        }

        public async void OnShowTrend(HierarchicalData data)
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


        public async void OnShowWork(HierarchicalData data, ColorScheme colorScheme)
        {
            var fileToAnalyze = data.Tag as string;
            var path = await _backgroundExecution.ExecuteAsync(() => _analyzer.AnalyzeWorkOnSingleFile(fileToAnalyze, colorScheme)).ConfigureAwait(true);

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

        private async void CommentsClick()
        {
            var comments = await _backgroundExecution.ExecuteAsync(_analyzer.ExportComments);
            _tabBuilder.ShowText<DataGridFriendlyComment>(comments, "Comments");
        }

        private EdgeData CreateEdgeData(Coupling coupling)
        {
            return new EdgeData(coupling.Item1, coupling.Item2, coupling.Degree)
                   {
                           Node1DisplayName = GetVertexName(coupling.Item1),
                           Node2DisplayName = GetVertexName(coupling.Item2)
                   };
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
            var dataContext = await _backgroundExecution.ExecuteAsync(_analyzer.AnalyzeHotspots);

            _tabBuilder.ShowHierarchicalData(dataContext, "Hotspots");
            _tabBuilder.ShowWarnings(_analyzer.Warnings);
        }

        private async void KnowledgeClick()
        {
            var directory = _dialogs.GetDirectory(_project.ProjectBase);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            if (!directory.StartsWith(_project.ProjectBase, StringComparison.OrdinalIgnoreCase))
            {
                _dialogs.ShowError(Strings.ErrorDirectoryNotInProjectBase);
                return;
            }

            var data = await _backgroundExecution.ExecuteAsync(() => _analyzer.AnalyzeKnowledge(directory));
            _tabBuilder.ShowHierarchicalData(data, "Knowledge");
        }

        private void LoadDataClick()
        {
            try
            {
                var fileName = _dialogs.GetLoadFile("bin", _project.Cache);
                if (fileName != null)
                {
                    var file = new BinaryFile<HierarchicalData>();
                    var data = file.Read(fileName);

                    var keys = new List<string>();
                    data.TraverseBottomUp(x =>
                                          {
                                              if (x.IsLeafNode)
                                              {
                                                  if (x.ColorKey != null)
                                                  {
                                                      keys.Add(x.ColorKey);
                                                  }
                                              }
                                          });

                    var colorScheme = new ColorScheme();

                    // Rebuild color scheme if it was used
                    var distinctKeys = keys.Distinct().ToArray();
                    if (distinctKeys.Any())
                    {
                        colorScheme = new ColorScheme(distinctKeys);
                    }

                    var context = new HierarchicalDataContext(data, colorScheme);
                    _tabBuilder.ShowHierarchicalData(context, "Loaded");
                }
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(ex.Message);
            }
        }


        private void Save(string fileName, HierarchicalData data)
        {
            var file = new BinaryFile<HierarchicalData>();
            file.Write(fileName, data);
        }

        private void SaveDataClick()
        {
            // TODO Save other stuff
            if (Tabs.Any() == false || SelectedIndex < 0)
            {
                return;
            }

            var descr = Tabs.ElementAt(SelectedIndex);

            // Saving hierarchical data
            if (descr.Data is HierarchicalDataContext context)
            {
                var fileName = _dialogs.GetSaveFile("bin", _project.Cache);
                if (fileName != null)
                {
                    Save(fileName, context.Data);
                }
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
            _tabBuilder.ShowText<DataGridFriendlyArtifact>(summary, "Summary");
        }

        private async void UpdateClick()
        {
            // The functions to update or pull are implemented in SvnProvider and GitProvider.
            // But actually that is not the task of this tool. Give it an updated repository.

            if (!_dialogs.AskYesNoQuestion(Strings.SyncInstructions, "Confirm"))
            {
                return;
            }

            await _backgroundExecution.ExecuteAsync(() => _analyzer.UpdateCache());
            _analyzer.Clear();
        }

        private async void WorkOnSingleFileClick()
        {
            // Called from ribbon. Select a file and show the work.
            // Colors are assinged on the fly.

            var fileName = _dialogs.GetLoadFile(null, _project.ProjectBase);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var path = await _backgroundExecution.ExecuteAsync(() => _analyzer.AnalyzeWorkOnSingleFile(fileName, new ColorScheme()));

            _tabBuilder.ShowImage(new BitmapImage(new Uri(path)));
        }
    }
}