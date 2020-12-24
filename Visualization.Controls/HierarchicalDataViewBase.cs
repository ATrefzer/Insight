﻿using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Visualization.Controls.Common;
using Visualization.Controls.Data;
using Visualization.Controls.Drawing;
using Visualization.Controls.Interfaces;
using Visualization.Controls.Tools;

namespace Visualization.Controls
{
    /// <summary>
    /// Note about the coloring:
    /// Filtering is always done on a clone of the original tree. Filtering leads to recalculation of the weights (colors).
    /// This happens when we change the filters in the tool window.
    /// Changing the zoom level only sets a different entry point to display. It does not recalculate the weights.
    /// Therefore, the zoom level does not affect the coloring even if the data with the most significant weights are not visible.
    /// </summary>
    public abstract class HierarchicalDataViewBase : UserControl
    {
        public static readonly DependencyProperty UserCommandsProperty = DependencyProperty.Register(
            "UserCommands", typeof(HierarchicalDataCommands), typeof(HierarchicalDataViewBase),
            new PropertyMetadata(null));

        private readonly HitTest _hitTest = new HitTest();

        protected readonly MenuItem _toolMenuItem = new MenuItem {Header = "Tools", Tag = null};
        protected IBrushFactory _brushFactory;

        protected IRenderer _renderer;

        /// <summary>
        ///     Original data, untouched
        /// </summary>
        protected IHierarchicalData _originalData;


        protected ToolViewModel _toolViewModel;

        /// <summary>
        ///     Sub tree to display. This is the current zoom level shown.
        ///     The weights (colors) are not adjusted when we change the zoom level.
        ///     This may be a clone of the original data (when filtered) or
        ///     a reference to the original data (no filter)
        /// </summary>
        protected IHierarchicalData _zoomLevel;

        protected ToolView ToolView;


        /// <summary>
        ///     Commands that apply to leaf nodes of a hierarchical data.
        /// </summary>
        public HierarchicalDataCommands UserCommands
        {
            get => (HierarchicalDataCommands) GetValue(UserCommandsProperty);
            set => SetValue(UserCommandsProperty, value);
        }

        protected void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _originalData = null;
            _brushFactory = null;

            if (!(DataContext is HierarchicalDataContext context) || context.Data == null)
            {
                // This is called once with the wrong context.
                return;
            }

            _brushFactory = context.BrushFactory;
            _originalData = context.Data;

            InitializeTools(context.AreaSemantic, context.WeightSemantic);

            // TODO Cleanup Extract Ids and add layout infos.
            // Should not be part of the hierarchical data.
            var id = 1;
            _originalData.TraverseBottomUp(node => node.Id = id++);

            // Initially no filtering so skip removing nodes.
            ZoomLevelChanged(_originalData);
        }

        protected void OnToolHighlightPatternChanged(object sender, EventArgs args)
        {
            // Render again with new highlighting
            DoRender(_zoomLevel);
        }

        protected void ChangeZoomLevelCommand(IHierarchicalData item)
        {
            if (item == null)
            {
                return;
            }

            // Note: source tree is already filtered.
            ZoomLevelChanged(item);
        }

        protected abstract void ClosePopup();

        protected abstract IRenderer CreateRenderer();

        private IHierarchicalData DoFilter(IHierarchicalData data)
        {
            // TODO move outside
            if (_toolViewModel.NoFilterJustHighlight)
            {
                // Highlighting the filter instead of removing the nodes.
                return data;
            }

            data.RemoveLeafNodes(leaf =>
                !_toolViewModel.IsAreaValid(leaf.AreaMetric) ||
                !_toolViewModel.IsWeightValid(leaf.WeightMetric));

            try
            {
                data.RemoveLeafNodesWithoutArea();
            }
            catch (Exception)
            {
                data = HierarchicalData.NoData();
            }

            // After we removed weights we have to normalize again.
            data.SumAreaMetrics(); // Only TreeMapView
            data.NormalizeWeightMetrics();

            return data;
        }

        protected void OnToolFilterChanged(object sender, EventArgs args)
        {
            var oldZoomLevelId = _zoomLevel?.Id;
            var sourceData = DoFilter(_originalData.Clone());
           
            var zoomTo = FindById(sourceData, oldZoomLevelId);

            ZoomLevelChanged(zoomTo);
        }

        private IHierarchicalData FindById(IHierarchicalData tree, int? id)
        {
            var zoomTo = tree;
            if (!id.HasValue)
            {
                return zoomTo;
            }

            var idValue = id.Value;
            var newZoom = tree.FirstOrDefault(node => node.Id == idValue);
            if (newZoom != null)
            {
                zoomTo = newZoom;
            }

            return zoomTo;
        }


        protected abstract DrawingCanvas GetCanvas();


        protected void HideToolView()
        {
            // When the control is no longer visible close the tool window.
            ToolView?.Close();
            _zoomLevel = null;
        }


        protected void InitializeTools(string areaSemantic, string weightSemantic)
        {
            var area = new HashSet<double>();
            var weight = new HashSet<double>();

            // Distinct areas and weights. Each slider tick goes to the next value.
            // This allows smooth navigation even if there are large outliers.
            _originalData.TraverseTopDown(data =>
            {
                if (data.IsLeafNode)
                {
                    area.Add(data.AreaMetric);
                    weight.Add(data.WeightMetric);
                }
            });

            var areaList = area.OrderBy(x => x).ToList();
            var weightList = weight.OrderBy(x => x).ToList();

            _toolViewModel = new ToolViewModel(areaList, weightList);
            _toolViewModel.AreaSemantic = areaSemantic;
            _toolViewModel.WeightSemantic = weightSemantic;

            _toolViewModel.FilterChanged += OnToolFilterChanged;
            _toolViewModel.HighlightPatternChanged += OnToolHighlightPatternChanged;
            _toolViewModel.Reset += OnToolReset;
        }

        private void OnToolReset(object sender, EventArgs e)
        {
            ZoomLevelChanged(_originalData);
        }


        protected abstract void InitPopup(IHierarchicalData hit);

        protected virtual ContextMenu GetContextMenu(object sender)
        {
            var fe = sender as FrameworkElement;
            return fe?.ContextMenu;
        }

        protected void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Does not tell which one.

            var canvas = GetCanvas();
            var pos = _renderer.Transform(Mouse.GetPosition(canvas));
            var hit = _hitTest.Hit(_zoomLevel, pos);
            var menu = GetContextMenu(sender);
            if (hit != null && menu != null)
            {
                menu.Items.Clear();

                // Item for filter tool window
                _toolMenuItem.IsEnabled = ToolView == null || !ToolView.IsVisible;
                _toolMenuItem.Command = new DelegateCommand(ShowToolsCommand);
                menu.Items.Add(_toolMenuItem);

                UserCommands?.Fill(menu, hit);

                menu.Items.Add(new Separator());

                FillZoomLevels(menu, hit);
            }

            // Show context menu if at least one item is there.
            e.Handled = menu?.Items.Count == 0;
        }


        protected void ShowToolsCommand()
        {
            // Filter
            ToolView = new ToolView();
            ToolView.Owner = Application.Current.MainWindow;
            ToolView.DataContext = _toolViewModel;
            ToolView.Show();
        }


        protected void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            ClosePopup();
        }


        protected void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_originalData == null)
            {
                return;
            }

            ClosePopup();

            // Circle packing renderer uses transformations. So we have to translate the mouse position
            // into the coordinates of the circles.
            var pos = _renderer.Transform(e.GetPosition(GetCanvas()));
            var hit = _hitTest.Hit(_zoomLevel, pos);
            if (hit != null)
            {
                InitPopup(hit);
            }
        }

        protected void ZoomLevelChanged(IHierarchicalData data)
        {
            if (data == null)
            {
                return;
            }

            DoRender(data);
        }

        private void DoRender(IHierarchicalData data)
        {
            _zoomLevel = data;
            _renderer = CreateRenderer();
            _renderer.LoadData(_zoomLevel);
            _renderer.Highlighting = new Highlighting(_toolViewModel);
            GetCanvas().DataContext = _renderer;
        }

        private void AddZoomLevel(ContextMenu menu, IHierarchicalData data)
        {
            var header = data.GetPathToRoot();
            var menuItem = new MenuItem {Header = header};
            menuItem.Command = new DelegateCommand(() => ChangeZoomLevelCommand(data));
            menu.Items.Add(menuItem);
        }

        private void FillZoomLevels(ContextMenu menu, IHierarchicalData hit)
        {
            // From the current item (exclusive) up the the root 
            // add an context menu entry for each zoom level.

            var current = hit;
            while (current != null)
            {
                // Avoid unnecessary context menus
                if (current != _zoomLevel && current.IsLeafNode == false)
                {
                    AddZoomLevel(menu, current);
                }

                current = current.Parent;
            }
        }
    }
}