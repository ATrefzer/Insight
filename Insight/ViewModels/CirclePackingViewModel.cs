using Visualization.Controls;

namespace Insight.ViewModels
{
    public sealed class CirclePackingViewModel : TabContentViewModel
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