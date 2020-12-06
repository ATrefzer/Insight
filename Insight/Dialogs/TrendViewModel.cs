using System.Collections.Generic;

using Insight.Metrics;
using Insight.WpfCore;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Insight.Dialogs
{
    internal sealed class TrendViewModel : ViewModelBase
    {
        private PlotModel _selectedPlotModel;

        public TrendViewModel(List<TrendData> ordered)
        {
            Models = new List<PlotModel>
                     {
                             CreateCodeLinesModel(ordered),
                             CreateComplexityModel(ordered)
                     };

            PlotModel = Models[0];
        }

        public List<PlotModel> Models { get; set; }

        public PlotModel PlotModel
        {
            get => _selectedPlotModel;
            set
            {
                _selectedPlotModel = value;
                OnPropertyChanged();
            }
        }

        private static void CreateAxes(PlotModel pm, string yTitle)
        {
            var dateAxis = new DateTimeAxis();
            dateAxis.Position = AxisPosition.Bottom;
            dateAxis.StringFormat = "yyyy-MM-dd";
            dateAxis.MajorGridlineStyle = LineStyle.Solid;
            dateAxis.MinorGridlineStyle = LineStyle.Dot;

            //dateAxis.Maximum = DateTimeAxis.ToDouble(DateTime.Now.AddDays(1));
            pm.Axes.Add(dateAxis);

            var valueAxis = new LinearAxis();
            valueAxis.Position = AxisPosition.Left;
            valueAxis.MajorGridlineStyle = LineStyle.Solid;
            valueAxis.MinorGridlineStyle = LineStyle.Dot;
            valueAxis.Title = yTitle;
            valueAxis.Minimum = 0.0;
            valueAxis.MaximumPadding = 0.1;
            pm.Axes.Add(valueAxis);
        }

        private static void CreateLegend(PlotModel pm)
        {
            pm.LegendTitle = "Legend";
            pm.LegendOrientation = LegendOrientation.Horizontal;
            pm.LegendPlacement = LegendPlacement.Outside;
            pm.LegendPosition = LegendPosition.TopRight;
            pm.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
            pm.LegendBorder = OxyColors.Black;
        }

        private PlotModel CreateCodeLinesModel(List<TrendData> trendData)
        {
            var pm = new PlotModel();
            pm.Title = "Lines of Code";

            var linesOfCode = new LineSeries
                              {
                                      Color = OxyColor.FromRgb(0, 0, 255),
                                      StrokeThickness = 2,
                                      MarkerSize = 3,
                                      CanTrackerInterpolatePoints = false,
                                      Smooth = false,
                                      Title = Strings.LinesOfCode
                              };

            var linesOfComment = new LineSeries
                                 {
                                         Color = OxyColor.FromRgb(0, 255, 0),
                                         StrokeThickness = 2,
                                         MarkerSize = 3,
                                         CanTrackerInterpolatePoints = false,
                                         Smooth = false,
                                         Title = Strings.Comments
                                 };

            foreach (var data in trendData)
            {
                var dp = new DataPoint(DateTimeAxis.ToDouble(data.Date), data.Loc.Code);
                linesOfCode.Points.Add(dp);

                dp = new DataPoint(DateTimeAxis.ToDouble(data.Date), data.Loc.Comments);
                linesOfComment.Points.Add(dp);
            }

            CreateLegend(pm);
            CreateAxes(pm, "Lines");
            pm.Series.Add(linesOfCode);
            pm.Series.Add(linesOfComment);
            return pm;
        }

        private PlotModel CreateComplexityModel(List<TrendData> trendData)
        {
            var pm = new PlotModel();
            pm.Title = "Complexity (logical spaces)";

            var average = new LineSeries
                          {
                                  Color = OxyColor.FromRgb(0, 0, 255),
                                  StrokeThickness = 2,
                                  MarkerSize = 3,
                                  CanTrackerInterpolatePoints = false,
                                  Smooth = false,
                                  Title = "Average"
                          };

            var stdDev = new LineSeries
                         {
                                 Color = OxyColor.FromRgb(0, 255, 0),
                                 StrokeThickness = 2,
                                 MarkerSize = 3,
                                 CanTrackerInterpolatePoints = false,
                                 Smooth = false,
                                 Title = "Standard Deviation"
                         };

            foreach (var data in trendData)
            {
                var dp = new DataPoint(DateTimeAxis.ToDouble(data.Date), data.InvertedSpace.Mean);
                average.Points.Add(dp);

                dp = new DataPoint(DateTimeAxis.ToDouble(data.Date), data.InvertedSpace.StandardDeviation);
                stdDev.Points.Add(dp);
            }

            CreateLegend(pm);
            CreateAxes(pm, "Complexity");
            pm.Series.Add(average);
            pm.Series.Add(stdDev);
            return pm;
        }
    }
}