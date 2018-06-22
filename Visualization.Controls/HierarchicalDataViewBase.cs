using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Prism.Commands;

using Visualization.Controls.Data;
using Visualization.Controls.Drawing;
using Visualization.Controls.Interfaces;
using Visualization.Controls.Tools;

namespace Visualization.Controls
{
    public abstract class HierarchicalDataViewBase : UserControl
    {
        public static readonly DependencyProperty UserCommandsProperty = DependencyProperty.Register(
                                                                                                     "UserCommands", typeof(HierarchicalDataCommands), typeof(HierarchicalDataViewBase), new PropertyMetadata(null));

        protected readonly MenuItem _toolMenuItem = new MenuItem { Header = "Tools", Tag = null };
        protected ColorScheme _colorScheme;

        /// <summary>
        /// Filtered data
        /// </summary>
        protected HierarchicalData _filtered;

        protected IRenderer _renderer;

        /// <summary>
        /// Original data, untouched
        /// </summary>
        protected HierarchicalData _root;

        protected ToolViewModel _toolViewModel;

        /// <summary>
        /// Entry into _filtered data. This is the current level shown.
        /// </summary>
        protected HierarchicalData _zoomLevel;

        protected ToolView ToolView;

        /// <summary>
        /// Commands that apply to leaf nodes of a hierarchical data.
        /// </summary>
        public HierarchicalDataCommands UserCommands
        {
            get => (HierarchicalDataCommands)GetValue(UserCommandsProperty);
            set => SetValue(UserCommandsProperty, value);
        }

        protected void ChangeSearchHighlightingCommand(object sender, EventArgs args)
        {
            // Reuse zooming mechanism
            ZoomLevelChanged(_zoomLevel);
        }

        protected void ChangeZoomLevelCommand(HierarchicalData item)
        {
            if (item == null)
            {
                return;
            }

            ZoomLevelChanged(item);
        }

        protected abstract void ClosePopup();
        protected abstract IRenderer CreateRenderer();


        protected void FilterLevelChanged(object sender, EventArgs args)
        {
            _filtered = _root.Clone();
          
            if (_toolViewModel.NoFilterJustHighlight)
            {
                // Highlighting the filter instead of removing the nodes.
                ZoomLevelChanged(_filtered);
                return;
            }
            
            _filtered = _root.Clone();
            _filtered.RemoveLeafNodes(leaf =>
                !_toolViewModel.IsAreaValid(leaf.AreaMetric) ||
                !_toolViewModel.IsWeightValid(leaf.WeightMetric));

            try
            {
                _filtered.RemoveLeafNodesWithoutArea();
            }
            catch (Exception)
            {
                _filtered = HierarchicalData.NoData();
            }

            // After we removed weights we have to normalize again.
            _filtered.SumAreaMetrics(); // Only TreeMapView
            _filtered.NormalizeWeightMetrics();
            ZoomLevelChanged(_filtered);
        }


        protected abstract DrawingCanvas GetCanvas();


        protected void HideToolView()
        {
            // When the control is no longer visible close the tool window.
            ToolView?.Close();
            _filtered = null;
        }


        protected void InitializeTools()
        {
            var area = new HashSet<double>();
            var weight = new HashSet<double>();

            // Distinct areas and weights. Each slider tick goes to the next value.
            // This allows smooth naviation even if there are large outliers.
            _root.TraverseTopDown(data =>
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
            _toolViewModel.FilterChanged += FilterLevelChanged;
            _toolViewModel.SearchPatternChanged += ChangeSearchHighlightingCommand;
        }


        protected abstract void InitPopup(HierarchicalData hit);

        protected void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var canvas = GetCanvas();
            var pos = _renderer.Transform(Mouse.GetPosition(canvas));
            var hit = _zoomLevel.Hit(pos);
            var menu = canvas.ContextMenu;
            if ((hit != null) & (menu != null))
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

        protected void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _root = null;
            _colorScheme = null;

            if (!(DataContext is HierarchicalDataContext context) || context.Data == null)
            {
                // This is called once with the wrong context.
                return;
            }

            _colorScheme = context.ColorScheme;
            _root = context.Data;

            InitializeTools();

            // Initially no filtering so skip removing nodes.
            _filtered = _root;
            ZoomLevelChanged(_filtered);
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
            if (_root == null)
            {
                return;
            }

            ClosePopup();

            // Circle packing renderer uses transformations. So we have to translate the mouse position
            // into the coordinates of the circles.
            var pos = _renderer.Transform(e.GetPosition(GetCanvas()));
            var hit = _zoomLevel.Hit(pos);
            if (hit != null)
            {
                InitPopup(hit);
            }
        }

        protected void ZoomLevelChanged(HierarchicalData data)
        {
            if (data == null)
            {
                return;
            }

            _zoomLevel = data;
            _renderer = CreateRenderer();
            _renderer.LoadData(_zoomLevel);
            _renderer.Highlighing = new Highlighting(_toolViewModel);
            GetCanvas().DataContext = _renderer;
        }

        private void AddZoomLevel(ContextMenu menu, HierarchicalData data)
        {
            var header = data.GetPathToRoot();
            var menuItem = new MenuItem { Header = header };
            menuItem.Command = new DelegateCommand(() => ChangeZoomLevelCommand(data));
            menu.Items.Add(menuItem);
        }

        private void FillZoomLevels(ContextMenu menu, HierarchicalData hit)
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