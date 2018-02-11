using System.Collections;
using System.Windows;
using System.Windows.Controls;

using Visualization.Controls.Interfaces;

namespace Visualization.Controls
{
    /// <summary>
    /// Interaction logic for DataGridView.xaml
    /// </summary>
    public partial class DataGridView : UserControl
    {
        public DataGridView()
        {
            InitializeComponent();
            DataContextChanged += DataGridView_DataContextChanged;
        }

        private void DataGridView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _dataGrid.ItemsSource = DataContext as IEnumerable;
        }

        public IDataGridViewUserCommands UserCommands { get; set; }

        private void _dataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _contextMenu.Items.Clear();

            if (UserCommands == null)
            {
                e.Handled = true;
                return;
            }

            // Build the context menu
            var filled = UserCommands.Fill(_contextMenu, _dataGrid.SelectedItems);
            e.Handled = !filled;
        }
    }
}
