﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

using Visualization.Controls.Common;
using Visualization.Controls.Drawing;
using Visualization.Controls.Interfaces;
using Visualization.Controls.Tools;

namespace Visualization.Controls.CirclePacking
{
    internal sealed class CirclePackingRenderer : IRenderer
    {
        private readonly IBrushFactory _brushFactory;
        private IHierarchicalData _data;
        private GeneralTransform _inverse;
        private Pen _pen;

        public CirclePackingRenderer(IBrushFactory brushFactory)
        {
            _brushFactory = brushFactory;
        }

        public IHighlighting Highlighting { get; set; }

        public void LoadData(IHierarchicalData data)
        {
            _data = data;

            // Layout once. Later we scale it appropriately
            var layout = new CirclePackingLayout();
            layout.Layout(_data, double.MaxValue, double.MaxValue);
        }

        public void LoadRenderedData(IHierarchicalData data)
        {
            // Skip layouting
            _data = data;
        }

        public void RenderToDrawingContext(double actualWidth, double actualHeight, DrawingContext dc)
        {
            if (_data == null)
            {
                return;
            }

            var scale = GetScalingFactor(actualWidth, actualHeight);
            _pen = new Pen(new SolidColorBrush(Colors.Black), 1.0 / scale);
            _pen.Freeze();

            var centerOfWindow = new Point(actualWidth / 2.0, actualHeight / 2.0); //- (Vector)toplevelLayout.Center;

            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform(scale, scale));
            group.Children.Add(new ScaleTransform(1, -1));
            group.Children.Add(new TranslateTransform(centerOfWindow.X, centerOfWindow.Y));

            _inverse = group.Inverse;

            dc.PushTransform(group);

            Draw(dc, _data);

            dc.Pop();
        }

        public Point Transform(Point point)
        {
            if (_inverse == null)
            {
                return point;
            }

            return _inverse.Transform(point);
        }


        private void Draw(DrawingContext dc, IHierarchicalData data)
        {
            var brush = GetBrush(data);

            var layout = GetLayout(data);
            dc.DrawEllipse(brush, _pen, layout.Center, layout.Radius, layout.Radius);

            foreach (var child in data.Children)
            {
                Draw(dc, child);
            }
        }

        private SolidColorBrush GetBrush(IHierarchicalData data)
        {
            if (Highlighting != null && Highlighting.IsHighlighted(data))
            {
                return DefaultDrawingPrimitives.HighlightBrush;
            }

            SolidColorBrush brush;
            if (data.ColorKey != null)
            {
                return _brushFactory.GetBrush(data.ColorKey);
            }
            else
            {
                // For non leaf nodes the weight is 0. We only can merge area metrics.
                // See HierarchyBuilder.InsertLeaf.

                var color = DefaultDrawingPrimitives.WhiteToRedGradient.GradientStops.GetRelativeColor(data.NormalizedWeightMetric);
                brush = new SolidColorBrush(color);
                brush.Freeze();
            }

            return brush;
        }

        private CircularLayoutInfo GetLayout(IHierarchicalData item)
        {
            var layout = item.Layout as CircularLayoutInfo;
            Debug.Assert(layout != null);
            return layout;
        }

        private double GetScalingFactor(double actualWidth, double actualHeight)
        {
            var min = Math.Min(actualWidth, actualHeight) / 2.0;
            var scale = min / GetLayout(_data).Radius;
            return scale;
        }
    }
}