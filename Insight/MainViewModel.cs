using System.Collections.ObjectModel;
using System.Linq;

using Insight.WpfCore;

using Visualization.Controls;
using Visualization.Controls.Interfaces;

namespace Insight
{
    public class ViewDescription : ViewModelBase
    {
        private object _data;

        public object Data
        {
            get => _data;
            set
            {
                if (_data != value)
                {
                    _data = value;
                    OnPropertyChanged("Data");
                }
            }
        }

        public string Title { get; set; }
    }

    public class TreeMapDescription : ViewDescription
    {
        private HierarchicalDataCommands _commands;

        public HierarchicalDataCommands Commands
        {
            get { return _commands; }
            set
            {
                _commands = value;
                OnPropertyChanged();
            }
        }
    }

    public class CirclePackingDescription : ViewDescription
    {
        private HierarchicalDataCommands _commands;

        public HierarchicalDataCommands Commands
        {
            get { return _commands; }
            set
            {
                _commands = value; 
                OnPropertyChanged();
            }
        }
    }

    public class ImageDescription : ViewDescription
    {
    }

    public class ChordDescription : ViewDescription
    {
    }

    public class RawDataDescription : ViewDescription
    {
        public IDataGridViewUserCommands Commands { get; set; }
    }

    public sealed class MainViewModel : ViewModelBase
    {
        private readonly Project _project;

        private int _selectedIndex = -1;
        private ObservableCollection<ViewDescription> _tabs = new ObservableCollection<ViewDescription>();

        public MainViewModel(Project project)
        {
            _project = project;
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

        public ObservableCollection<ViewDescription> Tabs
        {
            get => _tabs;
            set
            {
                _tabs = value;
                OnPropertyChanged();
            }
        }

        public void Refresh()
        {
            OnAllPropertyChanged();
        }

        public void Show(ViewDescription description, bool toForeground)
        {
            var descr = _tabs.FirstOrDefault(d => d.Title == description.Title);
            var index = -1;
            if (descr != null)
            {
                // TODO commands are not updated. That should be ok
                descr.Data = description.Data;
                index = _tabs.IndexOf(descr);
            }
            else
            {
                Tabs.Add(description);
                index = Tabs.Count - 1;
            }

            if (toForeground || Tabs.Count == 1)
            {
                SelectedIndex = index;
            }
        }     
    }
}