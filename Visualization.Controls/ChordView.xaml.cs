using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Prism.Commands;

using Visualization.Controls.Chord;
using Visualization.Controls.Data;

using Label = Visualization.Controls.Chord.Label;

namespace Visualization.Controls
{
    /// <summary>
    /// Interaction logic for ChordView.xaml
    /// </summary>
    public sealed partial class ChordView
    {
        private List<EdgeData> _edgeData;
        private List<Edge> _edgeViewModels;

        private List<Label> _labelViewModels;
        private MainCircle _mainCircleViewModel;
        private double _maxLabelWidth = double.NaN;

        private Dictionary<string, Vertex> _vertexLookup;
        private List<Vertex> _vertexViewModels;

        public ChordView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
            SizeChanged += HandleSizeChanged;
        }

        public void MeasureLabel(string label, Dictionary<string, Size> labelToSize)
        {
            var size = CalculateSize(label);
            if (!labelToSize.ContainsKey(label))
            {
                labelToSize.Add(label, size);
            }
        }

        private static Visibility ToVisibility(bool isLabelVisible)
        {
            return isLabelVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BuildVisualTree()
        {
            Clear();

            var distinctLabels = new HashSet<string>();
            var labelToSize = new Dictionary<string, Size>();
            InitializeLabels(distinctLabels, labelToSize);

            CreateVertexAndLabelViewModels(distinctLabels, labelToSize);
            CreateEdgeViewModels();
            CreateMainCircleViewModel();
            CreateItemSource();
        }

        private Size CalculateSize(string text)
        {
            var lbl = new TextBlock();
            lbl.Text = text;
            lbl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = lbl.DesiredSize;

            // Limit the size
            size.Width = Math.Min(Constants.MaxLabelWidth, size.Width);            
            return size;
        }

        private void Clear()
        {
            _vertexViewModels = new List<Vertex>();
            _edgeViewModels = new List<Edge>();
            _labelViewModels = new List<Label>();
            _vertexLookup = null;
        }

        private void CreateEdgeViewModel(EdgeData edge)
        {
            var edgeViewModel = new Edge();

            edgeViewModel.IsSelected = false;
            edgeViewModel.Node1 = edge.Node1;
            edgeViewModel.Node2 = edge.Node2;

            _edgeViewModels.Add(edgeViewModel);
        }

        private void CreateEdgeViewModels()
        {
            foreach (var edge in _edgeData)
            {
                CreateEdgeViewModel(edge);
            }
        }

        private void CreateItemSource()
        {
            UpdateSize();

            var chordViewModels = new List<IChordElement>();
            chordViewModels.Add(_mainCircleViewModel);
            chordViewModels.AddRange(_edgeViewModels);
            chordViewModels.AddRange(_vertexViewModels);
            chordViewModels.AddRange(_labelViewModels);

            _shapes.ItemsSource = chordViewModels;
        }

        private void CreateLabelViewModel(string label, double angle, Size size)
        {
            var labelViewModel = new Label(label, angle, size);
            labelViewModel.MouseEnterCommand = new DelegateCommand(() => Select(labelViewModel));
            labelViewModel.MouseLeaveCommand = new DelegateCommand(() => Select((Vertex)null));
            _labelViewModels.Add(labelViewModel);
        }

        private void Select(Label labelViewModel)
        {
            // Finde vertex
            var vertex = _vertexLookup[labelViewModel.Text];
            Select(vertex);
        }

        private void CreateMainCircleViewModel()
        {
            _mainCircleViewModel = new MainCircle();
        }

        private void CreateVertexAndLabelViewModels(HashSet<string> distinctLabels, Dictionary<string, Size> labelToSize)
        {
            var angleStep = 2 * Math.PI / distinctLabels.Count;
            var angle = 0.0;

            // Calculate initial location of verticies and labels
            foreach (var label in distinctLabels)
            {
                CreateVertexViewModel(label, angle);
                CreateLabelViewModel(label, angle, labelToSize[label]);

                angle += angleStep;
            }

            _vertexLookup = _vertexViewModels.ToDictionary(x => x.Name, x => x);
        }

        /// <summary>
        /// Click on vertex allows to keep the highlighting stable.
        /// </summary>
        bool _isSelctedByClickOnVertex;

        private void CreateVertexViewModel(string nodeName, double angle)
        {
            var vertexViewModel = new Vertex(nodeName);
            vertexViewModel.SelectCommand = new DelegateCommand(() => 
            {
                _isSelctedByClickOnVertex = false;
                Select(vertexViewModel);
                _isSelctedByClickOnVertex = true;
            });
         

            vertexViewModel.Radius = 4;
            vertexViewModel.Angle = angle;
            _vertexViewModels.Add(vertexViewModel);

        }

        private string GetNodeLabel(string nodeName)
        {
            return nodeName;
        }

        private double GetRadiusOfMainCircle(double longestLabelWidth)
        {
            if (IsLabelVisible(longestLabelWidth))
            {
                return GetRadiusWithLabels(longestLabelWidth);
            }

            return GetRadiusWithoutLabels();
        }

        private double GetRadiusWithLabels(double longestLabelWidth)
        {
            var radius = Math.Min(ActualWidth / 2.0, ActualHeight / 2.0) - longestLabelWidth - 20;
            return radius;
        }

        private double GetRadiusWithoutLabels()
        {
            var radius = Math.Min(ActualWidth / 2.0, ActualHeight / 2.0) - 20;
            return radius;
        }

        private void HandleSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_edgeData != null)
            {
                UpdateSize();
            }
        }

        private void InitializeLabels(HashSet<string> distinctLabels, Dictionary<string, Size> labelToSize)
        {
            foreach (var edge in _edgeData)
            {
                var label1 = GetNodeLabel(edge.Node1);
                var label2 = GetNodeLabel(edge.Node2);
                distinctLabels.Add(label1);
                distinctLabels.Add(label2);

                // Measure all labels in advance such that we can calculate the radius.
                MeasureLabel(label1, labelToSize);
                MeasureLabel(label2, labelToSize);
            }

            InitializeMaxLabelWidth(labelToSize);
        }

        private void InitializeMaxLabelWidth(Dictionary<string, Size> labelToSize)
        {
            var maxHeight = labelToSize.Values.Select(x => x.Height).Max();
            var maxWidth = labelToSize.Values.Select(x => x.Width).Max();
            _maxLabelWidth = Math.Max(maxWidth, maxHeight);
        }

        private bool IsLabelVisible(double longestLabelWidth)
        {
            return GetRadiusWithLabels(longestLabelWidth) > 50;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            _edgeData = DataContext as List<EdgeData>;
            if (_edgeData != null)
            {
                BuildVisualTree();
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Click on any free space in the window releases the held selection.
            _isSelctedByClickOnVertex = false;
            Select((Vertex)null);
        }

        private void Select(Vertex vertex)
        {
            if (_isSelctedByClickOnVertex)
            {
                // Highlighting was started by click on vertex node.
                // You have to click again on a vertex or free space in the window to release.
                return;
            }

            foreach (var vertexViewModel in _vertexViewModels)
            {
                vertexViewModel.IsSelected = false;
            }


            foreach (var edge in _edgeViewModels)
            {
                if (vertex != null && (edge.Node1 == vertex.Name || edge.Node2 == vertex.Name))
                {
                    edge.IsSelected = true;

                    _vertexLookup[edge.Node1].IsSelected = true;
                    _vertexLookup[edge.Node2].IsSelected = true;
                }
                else
                {
                    edge.IsSelected = false;
                }
            }

            // Select label if node is selected and vice versa
            foreach (var labelViewModel in _labelViewModels)
            {
                labelViewModel.IsSelected = _vertexLookup[labelViewModel.Text].IsSelected;
            }
        }

        private void UpdateSize()
        {
            var radius = GetRadiusOfMainCircle(_maxLabelWidth);
            var isLabelVisible = IsLabelVisible(_maxLabelWidth);
            foreach (var vertex in _vertexViewModels)
            {
                vertex.UpdateLocation(radius);
            }

            foreach (var edge in _edgeViewModels)
            {
                edge.UpdateLocation(_vertexLookup);
            }

            foreach (var label in _labelViewModels)
            {
                // Only show labels if there is enough space.
                label.IsVisible = ToVisibility(isLabelVisible);
                label.UpdateLocation(radius);
            }

            _mainCircleViewModel.Radius = radius;
        }
    }
}