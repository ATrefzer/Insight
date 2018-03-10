using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Visualization.Controls.Tools
{
    public sealed class ToolViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Sorted list of all areas occurring in the data.
        /// </summary>
        List<double> _areas;

        /// <summary>
        /// Sorted list of all weights occurring in the data.
        /// </summary>
        List<double> _weights;

        int _minAreaIndex;
        int _maxAreaIndex;
        int _minWeightIndex;
        int _maxWeightIndex;

        /// <summary>
        /// Min area for user feedback.
        /// </summary>
        public double MinArea
        {
            get
            {
                return _areas[_minAreaIndex];
            }
        }

        /// <summary>
        /// Max area for user feedback.
        /// </summary>
        public double MaxArea
        {
            get
            {
                return _areas[_maxAreaIndex];
            }
        }

        /// <summary>
        /// Min weight for user feedback.
        /// </summary>
        public double MinWeight
        {
            get
            {
                return _weights[_minWeightIndex];
            }
        }

        /// <summary>
        /// Max weighta for user feedback.
        /// </summary>
        public double MaxWeight
        {
            get
            {
                return _weights[_maxWeightIndex];
            }
        }

        public int AreaIndexLower
        {
            get
            {
                return 0;
            }
        }
        public int AreaIndexUpper
        {
            get
            {
                return _areas.Count - 1;
            }
        }

        public int WeightIndexLower
        {
            get
            {
                return 0;
            }
        }
        public int WeightIndexUpper
        {
            get
            {
                return _weights.Count - 1;
            }
        }

        public ToolViewModel(List<double> areas, List<double> weights)
        {
            _areas = areas;
            _weights = weights;
            Reset();
        }

        public event EventHandler FilterChanged;
        public event EventHandler SearchPatternChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public int MaxAreaIndex
        {
            get => _maxAreaIndex;
            set
            {
                if (_maxAreaIndex != value)
                {
                    _maxAreaIndex = value;
                    
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MaxArea));
                    OnFilterChanged();
                }
            }
        }

        private string _searchPattern;
        public string SearchPattern
        {
            get => _searchPattern;
            set
            {
                if (_searchPattern != value)
                {
                    _searchPattern = value;
                    OnPropertyChanged();
                    OnSearchPatternChanged();
                }
            }
        }

        public int MaxWeightIndex
        {
            get => _maxWeightIndex;
            set
            {
                if (_maxWeightIndex != value)
                {
                    _maxWeightIndex = value;                    
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MaxWeight));
                    OnFilterChanged();
                }
            }
        }

        public int MinAreaIndex
        {
            get => _minAreaIndex;
            set
            {
                if (_minAreaIndex != value)
                {
                    _minAreaIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MinArea));
                    OnFilterChanged();
                }
            }
        }

        public int MinWeightIndex
        {
            get => _minWeightIndex;
            set
            {
                if (_minWeightIndex != value)
                {
                    _minWeightIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MinWeight));
                    OnFilterChanged();
                }
            }
        }

        public bool IsAreaValid(double area)
        {
            return area >= _areas[_minAreaIndex] &&
                 area <= _areas[_maxAreaIndex];
        }

        public bool IsWeightValid(double weight)
        {
            return weight >= _weights[_minWeightIndex] &&
                   weight <= _weights[_maxWeightIndex];
        }

        public void Reset()
        {
            MinWeightIndex = WeightIndexLower;
            MaxWeightIndex = WeightIndexUpper;
            MinAreaIndex = AreaIndexLower;
            MaxAreaIndex = AreaIndexUpper;
        }

        private void OnFilterChanged()
        {
            FilterChanged?.Invoke(this, new EventArgs());
        }

        private void OnSearchPatternChanged()
        {
            SearchPatternChanged?.Invoke(this, new EventArgs());
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}