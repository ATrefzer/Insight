using System;
using System.Windows;

namespace Insight
{
    public class ProgressService
    {
        private readonly MainWindow _mainWindow;

        public ProgressService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public Progress CreateProgress()
        {
            var progressView = new ProgressView();
            progressView.Owner = _mainWindow;
            progressView.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _mainWindow.IsEnabled = false;
            progressView.CanClose = false;
            _mainWindow.CanClose = false;

            progressView.Show();

            return new Progress(_mainWindow, progressView);
        }

        public sealed class Progress : IDisposable
        {
            private readonly MainWindow _mainWindow;
            private readonly ProgressView _progressView;

            public Progress(MainWindow mainWindow, ProgressView progressView)
            {
                _mainWindow = mainWindow;
                _progressView = progressView;
            }

            // TODO Update text!

            public void Dispose()
            {
                _mainWindow.IsEnabled = true;
                _progressView.CanClose = true;
                _mainWindow.CanClose = true;
                _progressView.Close();
            }
        }
    }
}