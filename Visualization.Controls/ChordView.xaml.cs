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

            var idToLabel = new Dictionary<string, string>();
            var labelToSize = new Dictionary<string, Size>();
            InitializeLabels(idToLabel, labelToSize);

            CreateVertexAndLabelViewModels(idToLabel, labelToSize);
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

            edgeViewModel.SelectCommand = new DelegateCommand(() =>
            {
                Select(edgeViewModel);
            });

            edgeViewModel.IsSelected = false;
            edgeViewModel.Node1Id = edge.Node1Id;
            edgeViewModel.Node2Id = edge.Node2Id;

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

        private void CreateLabelViewModel(string vertexId, string label, double angle, Size size)
        {
            var labelViewModel = new Label(vertexId, label, angle, size);
            labelViewModel.MouseEnterCommand = new DelegateCommand(() => Select(labelViewModel));
            labelViewModel.MouseLeaveCommand = new DelegateCommand(() => Select((Vertex) null));
            _labelViewModels.Add(labelViewModel);
        }

        private void CreateMainCircleViewModel()
        {
            _mainCircleViewModel = new MainCircle();
        }

        private void CreateVertexAndLabelViewModels(Dictionary<string, string> idToLabel, Dictionary<string, Size> labelToSize)
        {
            var angleStep = 2 * Math.PI / idToLabel.Keys.Count;
            var angle = 0.0;

            // Calculate initial location of verticies and labels
            foreach (var vertexInfo in idToLabel)
            {
                var label = vertexInfo.Value;
                var id = vertexInfo.Key;
                CreateVertexViewModel(id, label, angle);
                CreateLabelViewModel(id, label, angle, labelToSize[label]);

                angle += angleStep;
            }

            _vertexLookup = _vertexViewModels.ToDictionary(x => x.NodeId, x => x);
        }

        private void CreateVertexViewModel(string nodeId, string label, double angle)
        {
            var vertexViewModel = new Vertex(nodeId, label);
            vertexViewModel.SelectCommand = new DelegateCommand(() =>
                                                                {
                                                                    Select(vertexViewModel);
                                                                });

            vertexViewModel.Radius = 4;
            vertexViewModel.Angle = angle;
            _vertexViewModels.Add(vertexViewModel);
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

        private void InitializeLabels(Dictionary<string, string> distinctLabels, Dictionary<string, Size> labelToSize)
        {
            foreach (var edge in _edgeData)
            {
                var label1 = edge.Node1DisplayName;
                var label2 = edge.Node2DisplayName;

                // I asssume that for same ids the same display name is given!
                if (!distinctLabels.ContainsKey(edge.Node1Id))
                {
                    distinctLabels.Add(edge.Node1Id, label1);
                }

                if (!distinctLabels.ContainsKey(edge.Node2Id))
                {
                    distinctLabels.Add(edge.Node2Id, label2);
                }

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
            Select((Vertex) null);
        }

        private void Select(Label labelViewModel)
        {
            // Finde vertex
            var vertex = _vertexLookup[labelViewModel.VertexId];
            Select(vertex);
        }

        /// <summary>
        /// Selects the clicked edge and its two verticies.
        /// </summary>
        private void Select(Edge edge)
        {
            foreach (var vertexViewModel in _vertexViewModels)
            {
                vertexViewModel.IsSelected = false;
            }

            foreach (var edgeViewModel in _edgeViewModels)
            {
                edgeViewModel.IsSelected = false;
            }

            // Just select the clicked edge and the two vertiecies
            edge.IsSelected = true;
            _vertexLookup[edge.Node1Id].IsSelected = true;
            _vertexLookup[edge.Node2Id].IsSelected = true;
        }

        /// <summary>
        /// Selects a vertex and all edges / vertiecies attached to it.
        /// </summary>
        private void Select(Vertex vertex)
        {
            foreach (var vertexViewModel in _vertexViewModels)
            {
                vertexViewModel.IsSelected = false;
            }

            foreach (var edge in _edgeViewModels)
            {
                if (vertex != null && (edge.Node1Id == vertex.NodeId || edge.Node2Id == vertex.NodeId))
                {
                    edge.IsSelected = true;

                    _vertexLookup[edge.Node1Id].IsSelected = true;
                    _vertexLookup[edge.Node2Id].IsSelected = true;
                }
                else
                {
                    edge.IsSelected = false;
                }
            }

            // Select label if node is selected and vice versa
            foreach (var labelViewModel in _labelViewModels)
            {
                labelViewModel.IsSelected = _vertexLookup[labelViewModel.VertexId].IsSelected;
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