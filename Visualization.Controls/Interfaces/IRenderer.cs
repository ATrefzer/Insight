using System.Windows;
using System.Windows.Media;

using Visualization.Controls.Data;
using Visualization.Controls.Tools;

namespace Visualization.Controls.Interfaces
{
    public interface IRenderer
    {
        void RenderToDrawingContext(double actualWidth, double actualHeight, DrawingContext dc);
    
        void LoadData(HierarchicalData zoomLevel);
        Point Transform(Point mousePosition);

        IHighlighting Highlighing { get; set; }
    }
}