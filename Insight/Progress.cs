using System;
using System.Windows;

using Insight.Dialogs;

namespace Insight
{
    public sealed class Progress : IDisposable
    {
        private readonly MainWindow _mainWindow;
        private readonly ProgressView _progressView;

        public Progress(MainWindow mainWindow, ProgressView progressView)
        {
            _mainWindow = mainWindow;
            _progressView = progressView;
        }

        public void Message(string msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
                                                  {
                                                      _progressView.Message.Visibility = Visibility.Visible;
                                                      _progressView.Message.Text = msg;
                                                      _progressView.SizeToContent = SizeToContent.Height;
                                                  });
        }

        public void Dispose()
        {
            _mainWindow.IsEnabled = true;
            _progressView.CanClose = true;
            _mainWindow.CanClose = true;
            _progressView.Close();
        }
    }
}