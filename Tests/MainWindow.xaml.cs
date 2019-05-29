using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

using Insight.Shared;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using Visualization.Controls;
using Visualization.Controls.Data;

namespace Tests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public PlotModel PlotModel
        {
            get
            {
                var pm = new PlotModel();
                pm.LegendTitle = "Legend";
                pm.LegendOrientation = LegendOrientation.Horizontal;
                pm.LegendPlacement = LegendPlacement.Outside;
                pm.LegendPosition = LegendPosition.TopRight;
                pm.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
                pm.LegendBorder = OxyColors.Black;

                var dateAxis = new DateTimeAxis();
                dateAxis.Position = AxisPosition.Bottom;
                dateAxis.StringFormat = "yyyy-MM-dd";
                dateAxis.MajorGridlineStyle = LineStyle.Solid;
                dateAxis.MinorGridlineStyle = LineStyle.Dot;

                //   dateAxis.IntervalLength = 80;
                pm.Axes.Add(dateAxis);

                var valueAxis = new LinearAxis();
                valueAxis.Position = AxisPosition.Left;
                valueAxis.MajorGridlineStyle = LineStyle.Solid;
                valueAxis.MinorGridlineStyle = LineStyle.Dot;
                valueAxis.Title = "Value";
                valueAxis.Minimum = 0.0;
                pm.Axes.Add(valueAxis);

                var lineSerie = new LineSeries
                {
                    StrokeThickness = 2,
                    MarkerSize = 3,
                    MarkerStroke = OxyColor.FromRgb(255, 0, 0),

                    //    MarkerStroke = colors[data.Key],
                    //   MarkerType = markerTypes[data.Key],
                    CanTrackerInterpolatePoints = false,
                    Title = "for the legend",
                    Smooth = false
                };

                var dt = DateTime.Now;
                for (var i = 100; i < 130; i++)
                {
                    dt = dt.AddDays(1);
                    var dp = new DataPoint(DateTimeAxis.ToDouble(dt), i);
                    lineSerie.Points.Add(dp);
                }

                pm.Series.Add(lineSerie);
                return pm;
            }
        }

        private static List<EdgeData> GetChordTestData()
        {
            var edges = new List<EdgeData>();
            edges.Add(new EdgeData("AAAA", "B", 0.1));
            edges.Add(new EdgeData("A", "C", 0.1));
            edges.Add(new EdgeData("A", "D", 0.1));
            edges.Add(new EdgeData("B", "C", 0.1));
            edges.Add(new EdgeData("B", "D", 0.1));
            edges.Add(new EdgeData("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC", "D", 0.1));
            edges.Add(new EdgeData("C", "K", 0.1));
            edges.Add(new EdgeData("C", "L", 0.1));
            edges.Add(new EdgeData("C", "M", 0.1));
            edges.Add(new EdgeData("C", "N", 0.1));
            return edges;
        }

        private HierarchicalDataContext LoadCached(string cacheFile, string fileName)
        {
            var builder = new HierarchicalDataBuilder();

            //var cacheFile = "d:\\data.bin";
            if (File.Exists(cacheFile))
            {
                var file = new BinaryFile<HierarchicalData>();
                var data = file.Read(cacheFile);
                return new HierarchicalDataContext(data);
            }
            else
            {
                var context = builder.CreateHierarchyFromFilesystem(fileName, true);
                var file = new BinaryFile<HierarchicalData>();
                file.Write(cacheFile, context.Data as HierarchicalData);
                return context;
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Fill sample data to visualiue
            var builder = new HierarchicalDataBuilder();

            _chord.DataContext = GetChordTestData();

            var circle = builder.GetFlatExample();
            var tree = builder.GetFlatExample();

            _circlePacking.DataContext = circle;
            _treeMap.DataContext = tree;
        }
    }
}