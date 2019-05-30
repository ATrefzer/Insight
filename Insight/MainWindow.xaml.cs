using Insight.Metrics;
using System.ComponentModel;
using System.Windows;

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

            // Last know project
            var project = Application.Current.Properties["project"] as Project;

            var viewController = new ViewController(this);
            var analyzer = new Analyzer(project, new MetricProvider());
            var dialogs = new DialogService();
            var progressService = new ProgressService(this);
            var backgroundExecution = new BackgroundExecution(progressService, dialogs);

            var mainViewModel = new MainViewModel(viewController, dialogs, project, analyzer, backgroundExecution);
            DataContext = mainViewModel;
        }

        public bool CanClose { get; set; } = true;

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = !CanClose;
        }
    }
}