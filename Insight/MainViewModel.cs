using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using Insight.ViewModels;
using Insight.WpfCore;

using Prism.Commands;

namespace Insight
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly Analyzer _analyzer;
        private readonly BackgroundExecution _backgroundExecution;

        // TODO needed?
        private readonly Project _project;
        private readonly ViewController _viewController;

        private int _selectedIndex = -1;
        private ObservableCollection<TabContentViewModel> _tabs = new ObservableCollection<TabContentViewModel>();

        public MainViewModel(ViewController viewController, Project project, Analyzer analyzer, BackgroundExecution backgroundExecution)
        {
            _viewController = viewController;
            _project = project;
            _analyzer = analyzer;
            _backgroundExecution = backgroundExecution;

            SetupCommand = new DelegateCommand(SetupClick);
            UpdateCommand = new DelegateCommand(UpdateClick);
        }

        public bool IsProjectValid => _project.IsValid();

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                OnPropertyChanged();
            }
        }

        public ICommand SetupCommand { get; set; }

        public ObservableCollection<TabContentViewModel> Tabs
        {
            get => _tabs;
            set
            {
                _tabs = value;
                OnPropertyChanged();
            }
        }

        public DelegateCommand UpdateCommand { get; set; }

        public void Refresh()
        {
            OnAllPropertyChanged();
        }

        public void Show(TabContentViewModel info, bool toForeground)
        {
            var descr = _tabs.FirstOrDefault(d => d.Title == info.Title);
            var index = -1;
            if (descr != null)
            {
                index = _tabs.IndexOf(descr);
                _tabs.RemoveAt(index);
                _tabs.Insert(index, descr);
            }
            else
            {
                Tabs.Add(info);
                index = Tabs.Count - 1;
            }

            if (toForeground || Tabs.Count == 1)
            {
                SelectedIndex = index;
            }
        }

        private void SetupClick()
        {
            var changed = _viewController.ShowProjectSettings(_project);

            if (changed)
            {
                _tabs.Clear();
                _project.Save();
                _analyzer.Clear(); // TODO new project
            }

            // Refresh state of ribbon
            Refresh();
        }

        private async void UpdateClick()
        {
            // The functions to update or pull are implemented in SvnProvider and GitProvider.
            // But actually that is not the task of this tool. Give it an updated repository.

            if (!_viewController.AskYesNoQuestion(Strings.SyncInstructions, "Confirm"))
            {
                return;
            }

            await _backgroundExecution.ExecuteAsync(() => _analyzer.UpdateCache());
            _analyzer.Clear();
        }
    }
}