using System.ComponentModel;

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
    }
}