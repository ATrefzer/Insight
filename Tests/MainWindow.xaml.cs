using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                    MarkerStroke = OxyColor.FromRgb(255,0,0),
                    //    MarkerStroke = colors[data.Key],
                    //   MarkerType = markerTypes[data.Key],
                    CanTrackerInterpolatePoints = false,
                      Title = "for the legend",
                    Smooth = false,
                };

                var dt = DateTime.Now;
                for (int i = 100; i < 130; i++)
                {
                 
                    dt = dt.AddDays(1);
                    var dp = new DataPoint(DateTimeAxis.ToDouble(dt), i);
                    lineSerie.Points.Add(dp);
                }


                pm.Series.Add(lineSerie);
                    return pm;
            }
        }

        private HierarchicalData LoadCached(string cacheFile, string fileName)
        {
            var builder = new HierarchicalDataBuilder();

            //var cacheFile = "d:\\data.bin";
            HierarchicalData data;
            if (File.Exists(cacheFile))
            {
                var file = new BinaryFile<HierarchicalData>();
                data = file.Read(cacheFile);
            }
            else
            {
                data = builder.CreateHierarchyFromFilesystem(fileName, true);
                var file = new BinaryFile<HierarchicalData>();
                file.Write(cacheFile, data);
            }

            return data;
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


        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Fill sample data to visualiue
            var builder = new HierarchicalDataBuilder();


            _chord.DataContext = GetChordTestData();

            //_treeMap.DataContext = builder.CreateHierarchyFromFilesystem("d:\\downloads", true);

            //var circle = builder.GetColoredNestedExample();
            //var circle = LoadCached("all.bin", "d:\\");
            //var circle = LoadCached("downloads.bin", "d:\\Downloads");

            //var circle = LoadCached("downloads.bin", "d:\\Private");
            //var circle = LoadCached("sick_binaries.bin", "d:\\_Projekte\\Sick_binaries");
            //var circle = LoadCached("sick.bin", "d:\\_Projekte\\Sick");
            //Debug.WriteLine(circle.CountLeafNodes());

            //_circlePackaging.DataContext = circle;
        }
    }
}