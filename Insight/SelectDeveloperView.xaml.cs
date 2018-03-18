using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Insight
{
    /// <summary>
    /// Interaction logic for SelectDeveloperView.xaml
    /// </summary>
    public partial class SelectDeveloperView : Window
    {
        public SelectDeveloperView()
        {
            InitializeComponent();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        internal void SetDevelopers(List<string> mainDevelopers)
        {
            Developers.ItemsSource = mainDevelopers;
            Developers.SelectedIndex = 0;
        }

        internal string GetSelectedDeveloper()
        {
            return Developers.SelectedValue as string;
        }
    }
}
