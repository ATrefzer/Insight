using System.Windows;

using Insight.Properties;
using Insight.Shared;

namespace Insight
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private Project _project;

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            // Now saved on closing the dialog, too
            _project.Save();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            _project = new Project();
            _project.Load();

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
            
            Current.Properties.Add("project", _project);
        }
    }
}