using System.Windows;
using System.Windows.Media;

using Visualization.Controls.Data;
using Visualization.Controls.Drawing;
using Visualization.Controls.Interfaces;
using Visualization.Controls.Tools;

namespace Visualization.Controls.TreeMap
{
    public sealed class SquarifiedTreeMapRenderer : IRenderer
    {
        private HierarchicalData _data;

        // ReSharper disable once NotAccessedField.Local
        private int _level = -1;


        //private void DrawRectangle(Rect itemRect, object tag)
        //{
        //    // TODO Slow
        //    return; 
        //    var rect = new Rectangle();
        //    rect.Width = itemRect.Width;
        //    rect.Height = itemRect.Height;
        //    rect.Stroke = Brushes.Black;
        //    rect.StrokeThickness = 1;
        //    rect.Fill = Brushes.Red;
        //    Canvas.SetLeft(rect, itemRect.X);
        //    Canvas.SetTop(rect, itemRect.Y);
        //    rect.Tag = tag;
        //    _canvas.Children.Add(rect);
        //}


        /*
        internal void RenderToWritableBitmap(double actualWidth, double actualHeight, WriteableBitmap writeableBmp)
        {
            if (_data == null)
            {
                return;
            }

            _level = 0;
            RenderToWritableBitmap(writeableBmp, _data);
        }

       

        
        private void RenderToWritableBitmap(WriteableBitmap writeableBmp, HierarchicalData data)
        {
            _level++;
            if (data.Children.Count == 0)
            {
                writeableBmp.FillRectangle((int)data.Layout.Rect.TopLeft.X, (int)data.Layout.Rect.TopLeft.Y,
                    (int)data.Layout.Rect.BottomRight.X, (int)data.Layout.Rect.BottomRight.Y, Colors.Green);

                writeableBmp.DrawRectangle((int)data.Layout.Rect.TopLeft.X, (int)data.Layout.Rect.TopLeft.Y,
                    (int)data.Layout.Rect.BottomRight.X, (int)data.Layout.Rect.BottomRight.Y, Colors.Black);
            }
            foreach (var child in data.Children)
            {             
                RenderToWritableBitmap(writeableBmp, child);
            }

            _level--;
        }
        */


        public IHighlighting Highlighing { get; set; }

        /// <summary>
        /// Enusre that SumAreaMetrics and NomrmalizeWeightMetric was called and
        /// no node has an area of 0.
        /// </summary>
        public void LoadData(HierarchicalData data)
        {
            _data = data;
        }

        public void RenderToDrawingContext(double actualWidth, double actualHeight, DrawingContext dc)
        {
            if (_data == null)
            {
                return;
            }

            // Calculate the layout
            var layout = new SquarifiedTreeMapLayout();
            layout.Layout(_data, actualWidth, actualHeight);

            // Render to drawing context
            _level = 0;
            RenderToDrawingContext(dc, _data);
        }

        public Point Transform(Point mousePosition)
        {
            // We directly daw in screen coordinates.
            return mousePosition;
        }

        private static RectangularLayoutInfo GetLayout(HierarchicalData data)
        {
            return data.Layout as RectangularLayoutInfo;
        }

        private SolidColorBrush GetBrush(HierarchicalData data)
        {
            if (Highlighing != null && Highlighing.IsHighlighted(data))
            {
                return ColorScheme.Highlight;
            }

            SolidColorBrush brush;
            if (data.ColorKey != null)
            {
                brush = ColorScheme.GetBrush(data.ColorKey);
            }
            else
            {
                // For non leaf nodes the weight is 0. We only can merge area metrics.
                // See HiearchyBuilder.InsertLeaf.

                var color = ColorScheme.WhiteToRedGradient.GradientStops.GetRelativeColor(data.NormalizedWeightMetric);
                brush = new SolidColorBrush(color);
                brush.Freeze();
            }

            return brush;
        }


        private void RenderToDrawingContext(DrawingContext dc, HierarchicalData data)
        {
            _level++;
            if (data.IsLeafNode)
            {
                var brush = GetBrush(data);

                //dc.DrawRectangle(_gradient, _pen, data.Layout.Rect);
                var layout = GetLayout(data);
                dc.DrawRectangle(brush, ColorScheme.BlackPen, layout.Rect);
            }

            foreach (var child in data.Children)
            {
                RenderToDrawingContext(dc, child);
            }

            _level--;
        }
    }
}