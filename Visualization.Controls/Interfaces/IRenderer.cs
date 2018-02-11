using System.Windows;
using System.Windows.Media;

using Visualization.Controls.Data;

namespace Visualization.Controls.Interfaces
{
    public interface IRenderer
    {
        void RenderToDrawingContext(double actualWidth, double actualHeight, DrawingContext dc);
    
        void LoadData(HierarchicalData zoomLevel);
        Point Transform(Point mousePosition);
    }
}