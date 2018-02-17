using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using Insight.Metrics;
using Insight.Shared;
using Insight.Shared.Model;

using Visualization.Controls;
using Visualization.Controls.Data;

namespace Insight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow
    {
        private readonly Analyzer _analyzer;
        private readonly Dialogs _dialogs;
        private readonly Project _project;
        private bool _canClose = true;

        public MainWindow()
        {
            InitializeComponent();

            _circlePackaging.UserCommands = new HierarchicalDataCommands();
            _circlePackaging.UserCommands.Register("Trend", ShowTrend_Click);
            _circlePackaging.UserCommands.Register("Work", ShowWorkOnSingleFile_Click);

            _treeMap.UserCommands = new HierarchicalDataCommands();
            _treeMap.UserCommands.Register("Trend", ShowTrend_Click);
            _treeMap.UserCommands.Register("Work", ShowWorkOnSingleFile_Click);

            _project = Application.Current.Properties["project"] as Project;           
            _analyzer = new Analyzer(_project);
            _dialogs = new Dialogs();

            var viewModel = new MainViewModel(_project);
            DataContext = viewModel;
        }


        public async void ShowTrend_Click(HierarchicalData data)
        {
            var trendData = await ExecuteAsync(() => ExecuteShowTrendAsync(data));
            if (trendData == null)
            {
                // Exception was handled but there is not data.
                return;
            }

            var ordered = trendData.OrderBy(x => x.Date).ToList();
            var details = new TrendView();

            details.DataContext = new TrendViewModel(ordered);
            details.Owner = this;
            details.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            details.ShowDialog();
        }

        public async void ShowWorkOnSingleFile_Click(HierarchicalData data)
        {
            var fileToAnalyze = data.Tag as string;
            var path = await ExecuteAsync(() => _analyzer.AnalyzeWorkOnSingleFileAsync(fileToAnalyze)).ConfigureAwait(true);

            // Show image
            var viewer = new ImageView();
            viewer.SetImage(path);
            viewer.Owner = this;
            viewer.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            viewer.SizeToContent = SizeToContent.WidthAndHeight;
            viewer.ResizeMode = ResizeMode.NoResize;
            viewer.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var view = new AboutView();
            view.Owner = this;
            view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            view.ShowDialog();
        }

        private async void AnalyzeChangeCouplings_Click(object sender, RoutedEventArgs e)
        {
            var couplings = await ExecuteAsync(_analyzer.AnalyzeTemporalCouplingsAsync);

            // Context menu to show couplings in chord diagram
            var commands = new DataGridViewUserCommands<Coupling>();
            commands.Register(Strings.Visualize, args =>
            {
                if (args.Any())
                {
                    var edges = args.Select(coupling => new EdgeData(GetVertexName(coupling.Item1),
                                                                          GetVertexName(coupling.Item2),
                                                                          coupling.Degree));
                    _chord.DataContext = edges.ToList();
                    _tabControl.SelectedIndex = 4;
                }
            });

            _data.UserCommands = commands;
          


            _data.DataContext = couplings;
            _tabControl.SelectedIndex = 3;
        }

        private async void AnalyzeHotspots_Click(object sender, RoutedEventArgs e)
        {
            // Analyze hotspots from summary and code metrics
            var data = await ExecuteAsync(_analyzer.AnalyzeHotspotsAsync);
            ShowData(data);
        }

        private async void AnalyzeKnowledge_Click(object sender, RoutedEventArgs e)
        {
            var directory = _dialogs.GetDirectory(_project.ProjectBase);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            if (!directory.StartsWith(_project.ProjectBase, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"The directory is not contained in the project base. This is not allowed!");
                return;
            }

            var data = await ExecuteAsync(() => _analyzer.AnalyzeKnowledgeAsync(directory));
            ShowData(data);
        }

        private async Task ExecuteAsync(Func<Task> func)
        {
            var progress = new ProgressView();
            progress.Owner = this;
            progress.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            progress.Show();

            try
            {
                IsEnabled = false;
                progress.CanClose = false;
                _canClose = false;

                await func().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
                progress.CanClose = true;
                _canClose = true;
                progress.Close();
            }
        }

        private async Task<T> ExecuteAsync<T>(Func<Task<T>> func)
        {
            var progress = new ProgressView();
            progress.Owner = this;
            progress.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            progress.Show();

            var result = default(T);
            try
            {
                IsEnabled = false;
                progress.CanClose = false;
                _canClose = false;

                result = await func().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Enable main window
                IsEnabled = true;

                // Now we can close the progress (blockd due to ALT-F4)
                progress.CanClose = true;

                // Main window can be closed now
                _canClose = true;

                progress.Close();
            }

            return result;
        }

        private Task<List<TrendData>> ExecuteShowTrendAsync(HierarchicalData data)
        {
            return Task.Run(() =>
                            {
                                var trend = new List<TrendData>();
                                var localFile = data.Tag as string;

                                var svnProvider = _project.CreateProvider();

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
                            });
        }

        private async void ExportComments_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteAsync(_analyzer.ExportComments);
        }

        private async void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var summary = await ExecuteAsync(_analyzer.ExportSummary);
            _data.UserCommands = null;
            _data.DataContext = summary;
            _tabControl.SelectedIndex = 3;
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

        private void Load_Click(object sender, RoutedEventArgs e)
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

                // Rebuild color scheme if it was used
                var distinctKeys = keys.Distinct().ToArray();
                if (distinctKeys.Any())
                {
                    var mapper = new NameToColorMapper(distinctKeys);
                    ColorScheme.SetColorMapping(mapper);
                }

                ShowData(data);
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = !_canClose;
        }


        private void Save(string fileName, HierarchicalData data)
        {
            var file = new BinaryFile<HierarchicalData>();
            file.Write(fileName, data);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!(_treeMap.DataContext is HierarchicalData data))
            {
                return;
            }

            var fileName = _dialogs.GetSaveFile("bin", _project.Cache);
            if (fileName != null)
            {
                Save(fileName, data);
            }
        }

        private void Setup_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new ProjectViewModel(_project, new Dialogs());
            var view = new ProjectView();
            view.DataContext = viewModel;
            view.Owner = this;
            view.ShowDialog();

            // Refresh state of ribbon
            (DataContext as MainViewModel)?.Refresh();
        }

        private void ShowData(HierarchicalData data)
        {
            if (data == null)
            {
                return;
            }
            _treeMap.DataContext = data;
            _circlePackaging.DataContext = data.Clone();
            _tabControl.SelectedIndex = 0;
        }

        private async void ShowWorkOnSingleFile_Click(object sender, RoutedEventArgs e)
        {
            var fileName = _dialogs.GetLoadFile(null, _project.ProjectBase);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var path = await ExecuteAsync(() => _analyzer.AnalyzeWorkOnSingleFileAsync(fileName));

            // Show image
            _img.Source = new BitmapImage(new Uri(path));
            _tabControl.SelectedIndex = 2;
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            // The functions to update or pull are implemented in SvnProvider and GitProvider.
            // But actually that is not the task of this tool. Give it an updated repository.
            var msg = "Sync reads the version control's log and calculates code metrics for all supported files."
             + " This takes time. The data is persistently cached and used when doing the various analyses."
             + " If you synced before all cached data is deleted and rebuild."
             + " Best your repository is up to date and has no local changes."
             + " Do you want to proceed?";

            var result = MessageBox.Show(msg, "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            await ExecuteAsync(_analyzer.UpdateCacheAsyc);

            _analyzer.Clear();
        }
    }
}