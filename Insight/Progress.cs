using System;
using System.Windows;

using Insight.Dialogs;
using Insight.Shared;

namespace Insight
{
    public sealed class Progress : IDisposable, IProgress
    {
        private readonly MainWindow _mainWindow;
        private readonly ProgressView _progressView;

        public Progress(MainWindow mainWindow, ProgressView progressView)
        {
            _mainWindow = mainWindow;
            _progressView = progressView;
        }

        public void Dispose()
        {
            _mainWindow.IsEnabled = true;
            _progressView.CanClose = true;
            _mainWindow.CanClose = true;
            _progressView.Close();
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
    }
}