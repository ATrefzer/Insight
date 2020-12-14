using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Insight.ViewModels;

using Visualization.Controls.Common;

namespace Insight.Dialogs
{
    /// <summary>
    /// Interaction logic for ColorEditorView.xaml
    /// </summary>
    public sealed partial class ColorEditorView : Window
    {
        public ColorEditorView()
        {
            InitializeComponent();
        }


        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            if (DataContext is ISearchableViewModel filter)
            {
                // In window because of the collection view
                var view = (CollectionView) CollectionViewSource.GetDefaultView(_mappingView.ItemsSource);
                view.Filter = filter.CreateFilter(textBox.Text);
            }
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is ListBox mappingView)
            {
                if (mappingView.SelectedItems.Count > 1)
                {
                    // First item that was selected provides the color.
                    var mapping = _mappingView.SelectedItem as ColorMapping;
                    _mergeMenu.Header = $"Merge color with {mapping.Name}";
                    _mergeMenu.IsEnabled = true;
                }
                else
                {
                    _mergeMenu.Header =  Strings.MergeColor;
                    _mergeMenu.IsEnabled = false;
                }
            }

            
            
        }
    }
}