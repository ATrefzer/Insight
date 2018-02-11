using System.Windows;

namespace Visualization.Controls
{
    /// <summary>
    /// Interaction logic for FilterView.xaml
    /// </summary>
    public sealed partial class FilterView : Window
    {
        public FilterView()
        {
            InitializeComponent();
        }


        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is FilterViewModel model)
            {
                model.Reset();
            }
        }
    }
}