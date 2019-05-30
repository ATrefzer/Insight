using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Visualization.Controls.CirclePacking;
using Visualization.Controls.Drawing;
using Visualization.Controls.Interfaces;
using Visualization.Controls.Tools;

namespace Visualization.Controls
{
    /// <summary>
    /// Interaction logic for CirclePackingView.xaml
    /// </summary>
    public sealed partial class CirclePackingView : HierarchicalDataViewBase
    {
        public CirclePackingView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            ToolsExtension.Instance.ToolCloseRequested += Instance_ToolCloseRequested;
        }

        private void Instance_ToolCloseRequested(object sender, object e)
        {
            HideToolView();
        }

        protected override void ClosePopup()
        {
            _popup.IsOpen = false;
        }

        protected override IRenderer CreateRenderer()
        {
            return new CirclePackingRenderer(_colorScheme);
        }

        protected override DrawingCanvas GetCanvas()
        {
            return _canvasOrImage;
        }

        protected override void InitPopup(IHierarchicalData hit)
        {
            _popupText.Text = hit.Description;

            _popup.PlacementTarget = GetCanvas();
            _popup.Placement = PlacementMode.Mouse;
            _popup.Visibility = Visibility.Visible;
            _popup.IsOpen = true;
        }
    }
}