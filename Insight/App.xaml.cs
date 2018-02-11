using System.Windows;

namespace Insight
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private Project _project;

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            _project.Save();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            _project = new Project();
            _project.Load();
            Current.Properties.Add("project", _project);
        }
    }
}