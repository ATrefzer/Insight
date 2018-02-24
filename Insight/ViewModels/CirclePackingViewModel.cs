using Visualization.Controls;

namespace Insight.ViewModels
{
    public sealed class CirclePackingViewModel : TabContentViewModel
    {
        private HierarchicalDataCommands _commands;

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