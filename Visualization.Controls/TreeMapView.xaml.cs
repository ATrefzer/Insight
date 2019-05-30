using System.Windows;
using System.Windows.Controls.Primitives;

using Visualization.Controls.Data;
using Visualization.Controls.Drawing;
using Visualization.Controls.Interfaces;
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
        }

        protected override void ClosePopup()
        {
            _popup.IsOpen = false;
        }

        protected override IRenderer CreateRenderer()
        {
            return new SquarifiedTreeMapRenderer(_colorScheme);
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


        private void TreeMap_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            HideToolView();
        }
    }
}