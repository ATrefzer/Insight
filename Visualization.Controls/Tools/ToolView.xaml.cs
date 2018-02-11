using System.Windows;

namespace Visualization.Controls.Tools
{
    /// <summary>
    /// Interaction logic for ToolView.xaml
    /// </summary>
    public sealed partial class ToolView : Window
    {
        public ToolView()
        {
            InitializeComponent();
        }


        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToolViewModel model)
            {
                model.Reset();
            }
        }
    }
}