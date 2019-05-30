using Visualization.Controls;

namespace Insight.ViewModels
{
    /// <summary>
    /// ViewModel for the TreeMapView
    /// </summary>
    public sealed class TreeMapViewModel : TabContentViewModel
    {
        private HierarchicalDataCommands _commands;

        /// <summary>
        /// Note: Not every TabContentViewModel has HierarchicalDataCommands (for example: Summary Table)
        /// </summary>
        public HierarchicalDataCommands Commands
        {
            get => _commands;
            set
            {
                _commands = value;
                OnPropertyChanged();
            }
        }
    }
}