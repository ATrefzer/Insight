using System;
using System.Windows;

namespace Insight
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public sealed partial class AboutView
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void AboutView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var info = Application.GetResourceStream(new Uri("Resources/about.html", UriKind.Relative));
            if (info != null)
            {
                _browser.NavigateToStream(info.Stream);
            }
        }

        private void OnNavigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.Uri != null)
            {
                // Anything not from local resources.
                e.Cancel = true;
            }
        }
    }
}