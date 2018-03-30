using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Insight.Dialogs
{
    /// <summary>
    /// Interaction logic for SelectDeveloperView.xaml
    /// </summary>
    public sealed partial class SelectDeveloperView : Window
    {
        public SelectDeveloperView()
        {
            InitializeComponent();
        }

        internal string GetSelectedDeveloper()
        {
            return Developers.SelectedValue as string;
        }

        internal void SetDevelopers(List<string> mainDevelopers)
        {
            Developers.ItemsSource = mainDevelopers.OrderBy(x => x);
            Developers.SelectedIndex = 0;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}