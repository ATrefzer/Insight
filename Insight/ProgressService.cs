using System.Windows;

using Insight.Dialogs;

namespace Insight
{
    public sealed class ProgressService
    {
        private readonly MainWindow _mainWindow;

        public ProgressService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public Progress CreateProgress()
        {
            var progressView = new ProgressView { Owner = _mainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner, SizeToContent = SizeToContent.Height };

            _mainWindow.IsEnabled = false;
            progressView.CanClose = false;
            _mainWindow.CanClose = false;

            progressView.Show();

            return new Progress(_mainWindow, progressView);
        }
    }
}