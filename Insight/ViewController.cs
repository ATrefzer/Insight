using System.Windows;

namespace Insight
{
    public sealed class ViewController
    {
        private readonly MainWindow _mainWindow;

        public ViewController(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public bool AskYesNoQuestion(string msg, string caption)
        {
            var result = MessageBox.Show(msg, caption, MessageBoxButton.YesNo);
            return result == MessageBoxResult.Yes;
        }     

        public bool ShowProjectSettings(Project project)
        {
            var viewModel = new ProjectViewModel(project, new Dialogs());
            var view = new ProjectView();
            view.DataContext = viewModel;
            view.Owner = _mainWindow;
            var result = view.ShowDialog();
            return result.GetValueOrDefault() && viewModel.Changed;
        }
    }
}