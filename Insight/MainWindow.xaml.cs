using Insight.Metrics;
using System.ComponentModel;
using System.IO;
using System.Windows;

using Insight.Properties;

namespace Insight
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var lastKnownProject = Application.Current.Properties["lastKnownProject"] as string;

            var analyzer = new Analyzer(new MetricProvider());

            // If there is an last project load it immediately
            Project project = new Project();
            if (lastKnownProject != null && File.Exists(lastKnownProject))
            {
                project = new Project();
                project.Load(lastKnownProject);
                analyzer.Project = project;
            }

            var viewController = new ViewController(this);
           
            var dialogs = new DialogService();
            var progressService = new ProgressService(this);
            var backgroundExecution = new BackgroundExecution(progressService, dialogs);
            

            
            var mainViewModel = new MainViewModel(viewController, dialogs, backgroundExecution, analyzer, project);
            DataContext = mainViewModel;
        }

        public bool CanClose { get; set; } = true;

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = !CanClose;
        }
    }
}