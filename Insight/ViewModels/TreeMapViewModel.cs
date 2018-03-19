using Visualization.Controls;

namespace Insight.ViewModels
{
    public sealed class TreeMapViewModel : TabContentViewModel
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