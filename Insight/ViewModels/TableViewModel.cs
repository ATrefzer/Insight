using Visualization.Controls.Interfaces;

namespace Insight.ViewModels
{
    public sealed class TableViewModel : TabContentViewModel
    {
        public IDataGridViewUserCommands Commands { get; set; }
    }
}