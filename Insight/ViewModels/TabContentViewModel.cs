using Insight.WpfCore;

namespace Insight.ViewModels
{
    /// <summary>
    /// Base class for all view models that describe a tab item.
    /// </summary>
    public class TabContentViewModel : ViewModelBase
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
                    OnPropertyChanged();
                }
            }
        }

        public string Title { get; set; }
    }
}