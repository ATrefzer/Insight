using System.Collections;
using System.Windows.Controls;

namespace Visualization.Controls.Interfaces
{
    public interface IDataGridViewUserCommands
    {
        bool Fill(ContextMenu contextMenu, IEnumerable selectedItems);
    }
}
