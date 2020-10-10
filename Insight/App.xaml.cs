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
        private void App_OnExit(object sender, ExitEventArgs e)
        {
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

            Current.Properties.Add("lastKnownProject", Settings.Default.LastKnownProject);
        }
    }
}