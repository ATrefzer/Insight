using Insight.WpfCore;

namespace Insight.ViewModels
{
    /// <summary>
    /// ViewModel base class for all views we put into the tab control in the main window.
    /// </summary>
    public class TabContentViewModel : ViewModelBase
    {
        private object _data;

        /// <summary>
        /// The data context that is passed to the UserControl in Visualization.Controls.
        /// See MainWindow.xaml
        /// 
        /// We have different views like TreeMapView, ChordView etc. Therefore this is an object.
        /// Data is for example HierarchicalDataContext, List<EdgeData/>
        /// </summary>
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