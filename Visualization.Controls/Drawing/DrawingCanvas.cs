using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Visualization.Controls.Interfaces;

namespace Visualization.Controls.Drawing
{
    public sealed class DrawingCanvas : Canvas
    {
        public DrawingCanvas()
        {
            DataContextChanged += HandleDataContextChanged;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var data = DataContext as IRenderer;
            data?.RenderToDrawingContext(ActualWidth, ActualHeight, dc);
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Force new rendering
            InvalidateVisual();
        }
    }
}