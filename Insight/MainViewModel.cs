using Insight.WpfCore;

namespace Insight
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Project _project;

        public MainViewModel(Project project)
        {
            _project = project;
        }

        public bool IsProjectValid => _project.IsValid();

        public void Refresh()
        {
            OnAllPropertyChanged();
        }
    }
}