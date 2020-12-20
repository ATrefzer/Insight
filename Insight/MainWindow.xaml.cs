using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using Insight.ViewModels;

namespace Insight
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public bool CanClose { get; set; } = true;

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = !CanClose;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_tabControl.SelectedContent is TableViewModel vm)
            {
                if (!(sender is TextBox textBox))
                {
                    return;
                }
                
                var view = (CollectionView)CollectionViewSource.GetDefaultView(vm.Data);
                view.Filter = obj =>
                {
                    if (obj is ICanMatch canFilter)
                    {
                        return canFilter.IsMatch(textBox.Text);
                    }

                    // Cannot be filtered
                    return true;
                };

            }
        }
    }
}