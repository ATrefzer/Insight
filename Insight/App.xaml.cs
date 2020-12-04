using System.IO;
using System.Windows;

using Insight.Metrics;
using Insight.Properties;
using Insight.Shared;

namespace Insight
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private void App_OnExit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            // Load thresholds from config file

            // Summary
            Thresholds.MaxWorkItemsPerCommitForSummary = Settings.Default.MaxWorkItemsPerCommitForSummary;

            // Hotspots
            Thresholds.MinCommitsForHotspots = Settings.Default.MinCommitsForHotspots;
            Thresholds.MinLinesOfCodeForHotspot = Settings.Default.MinLinesOfCodeForHotspot;

            // Coupling
            Thresholds.MinCouplingForChangeCoupling = Settings.Default.MinCouplingForChangeCoupling;
            Thresholds.MaxItemsInChangesetForChangeCoupling = Settings.Default.MaxItemsInChangesetForChangeCoupling;
            Thresholds.MinDegreeForChangeCoupling = Settings.Default.MinDegreeForChangeCoupling;

            //Current.Properties.Add("lastKnownProject", Settings.Default.LastKnownProject);
            // var lastKnownProject = Application.Current.Properties["lastKnownProject"] as string;

            var lastKnownProject = Settings.Default.LastKnownProject;

            var analyzer = new Analyzer(new MetricProvider());

            // If there is an last project load it immediately
            Project project = new Project();
            if (lastKnownProject != null && File.Exists(lastKnownProject))
            {
                project = new Project();
                project.Load(lastKnownProject);
            }

            var mainWindow = new MainWindow();
            var viewController = new ViewController(mainWindow);
            var progressService = new ProgressService(mainWindow);
            var dialogs = new DialogService();
            var backgroundExecution = new BackgroundExecution(progressService, dialogs);


            var mainViewModel = new MainViewModel(viewController, dialogs, backgroundExecution, analyzer, project);
            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();
        }

     
    }
}