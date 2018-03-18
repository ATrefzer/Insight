using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Insight.Shared.Model;

namespace Insight
{
    public sealed class ViewController
    {
        private readonly MainWindow _mainWindow;

        public ViewController(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }      

        // TODO return a project
        public bool ShowProjectSettings(Project project)
        {
            var viewModel = new ProjectViewModel(project, new Dialogs());
            var view = new ProjectView();
            view.DataContext = viewModel;
            view.Owner = _mainWindow;
            var result = view.ShowDialog();
            return result.GetValueOrDefault() && viewModel.Changed;
        }

        public void ShowAbout()
        {
            var view = new AboutView();
            view.Owner = _mainWindow;
            view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            view.ShowDialog();
        }

        public void ShowImageViewer(string path)
        {
            // Show image
            var viewer = new ImageView();
            viewer.SetImage(path);
            viewer.Owner = _mainWindow;
            viewer.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            viewer.SizeToContent = SizeToContent.WidthAndHeight;
            viewer.ResizeMode = ResizeMode.NoResize;
            viewer.ShowDialog();
        }

        public void ShowTrendViewer(List<TrendData> trendOrderedByDate)
        {
            var details = new TrendView();
            details.DataContext = new TrendViewModel(trendOrderedByDate);
            details.Owner = _mainWindow;
            details.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            details.ShowDialog();
        }

        internal string SelectDeveloper(List<string> mainDevelopers)
        {
            if (!mainDevelopers.Any())
                return null;

            var view = new SelectDeveloperView();
            view.SetDevelopers(mainDevelopers);
            if (view.ShowDialog() == false)
            {
                return null;
            }
            return view.GetSelectedDeveloper();
        }
    }
}