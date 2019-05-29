using Visualization.Controls.Data;
using Visualization.Controls.Interfaces;

namespace Visualization.Controls.Tools
{
    public interface IHighlighting
    {
        bool IsHighlighted(IHierarchicalData data);
    }
}