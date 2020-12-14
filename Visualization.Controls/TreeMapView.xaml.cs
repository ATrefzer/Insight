using System.Windows;
using System.Windows.Controls.Primitives;

using Visualization.Controls.Drawing;
using Visualization.Controls.Interfaces;
using Visualization.Controls.Tools;
using Visualization.Controls.TreeMap;

namespace Visualization.Controls
{
    /// <summary>
    /// Interaction logic for TreeMapView.xaml
    /// </summary>
    public sealed partial class TreeMapView : HierarchicalDataViewBase
    {
        public TreeMapView()
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
            return new SquarifiedTreeMapRenderer(_brushFactory);
        }

        protected override DrawingCanvas GetCanvas()
        {
            return _canvasOrImage;
        }

        protected override void InitPopup(IHierarchicalData hit)
        {
            _popupText.Text = hit.Description;

            _popup.PlacementRectangle = ((RectangularLayoutInfo) hit.Layout).Rect;
            _popup.PlacementTarget = GetCanvas();
            _popup.Placement = PlacementMode.Mouse;
            _popup.Visibility = Visibility.Visible;
            _popup.IsOpen = true;
        }
    }
}