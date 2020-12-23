using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Prism.Commands;

using Visualization.Controls.Common;
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
        private HitTest _hitTest = new HitTest();
        protected readonly MenuItem _toolMenuItem = new MenuItem { Header = "Tools", Tag = null };
        protected IBrushFactory _brushFactory;

        /// <summary>
        /// Filtered data
        /// </summary>
        protected IHierarchicalData _filtered;

        protected IRenderer _renderer;

        /// <summary>
        /// Original data, untouched
        /// </summary>
        protected IHierarchicalData _root;

        protected ToolViewModel _toolViewModel;

        /// <summary>
        /// Entry into _filtered data. This is the current level shown.
        /// </summary>
        protected IHierarchicalData _zoomLevel;

        protected ToolView ToolView;

        /// <summary>
        /// Commands that apply to leaf nodes of a hierarchical data.
        /// </summary>
        public HierarchicalDataCommands UserCommands
        {
            get => (HierarchicalDataCommands)GetValue(UserCommandsProperty);
            set => SetValue(UserCommandsProperty, value);
        }

        protected void OnToolHighlightPatternChanged(object sender, EventArgs args)
        {
            // Reuse zooming mechanism
            ZoomLevelChanged(_zoomLevel);
        }

        protected void ChangeZoomLevelCommand(IHierarchicalData item)
        {
            if (item == null)
            {
                return;
            }

            ZoomLevelChanged(item);
        }

        protected abstract void ClosePopup();
        protected abstract IRenderer CreateRenderer();


        protected void OnToolFilterChanged(object sender, EventArgs args)
        {
            _filtered = _root.Clone();
          
            if (_toolViewModel.NoFilterJustHighlight)
            {
                // Highlighting the filter instead of removing the nodes.
                ZoomLevelChanged(_filtered);
                return;
            }
    
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

            // TODO We could keep the previous normalized weight metric such that the colors don't change

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


        protected void InitializeTools(string areaSemantic, string weightSemantic)
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
            _toolViewModel.AreaSemantic = areaSemantic;
            _toolViewModel.WeightSemantic = weightSemantic;

            _toolViewModel.FilterChanged += OnToolFilterChanged;
            _toolViewModel.HighlightPatternChanged += OnToolHighlightPatternChanged;
            _toolViewModel.Reset += OnToolReset;
        }

        private void OnToolReset(object sender, EventArgs e)
        {
            ZoomLevelChanged(_root);
        }


        protected abstract void InitPopup(IHierarchicalData hit);

        protected virtual ContextMenu GetContextMenu(object sender)
        {
            var fe = sender as FrameworkElement;
            return fe.ContextMenu;
        }

        protected void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Does not tell which one.

            var canvas = GetCanvas();
            var pos = _renderer.Transform(Mouse.GetPosition(canvas));
            var hit = _hitTest.Hit(_zoomLevel,pos);
            var menu = GetContextMenu(sender);
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
            _brushFactory = null;

            if (!(DataContext is HierarchicalDataContext context) || context.Data == null)
            {
                // This is called once with the wrong context.
                return;
            }
            
            _brushFactory = context.BrushFactory;
            _root = context.Data;

            InitializeTools(context.AreaSemantic, context.WeightSemantic);

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
            var hit = _hitTest.Hit(_zoomLevel,pos);
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

            _zoomLevel = data;
            _renderer = CreateRenderer();
            _renderer.LoadData(_zoomLevel);
            _renderer.Highlighting = new Highlighting(_toolViewModel);
            GetCanvas().DataContext = _renderer;
        }

        private void AddZoomLevel(ContextMenu menu, IHierarchicalData data)
        {
            var header = data.GetPathToRoot();
            var menuItem = new MenuItem { Header = header };
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