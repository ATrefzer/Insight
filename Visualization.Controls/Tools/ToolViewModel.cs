using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Visualization.Controls.Tools
{
    public sealed class ToolViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Round all double numbers shown to the user.
        /// </summary>
        private const int Digits = 4;

        /// <summary>
        /// Sorted list of all areas occurring in the data.
        /// </summary>
        private List<double> _areas;

        /// <summary>
        /// Sorted list of all weights occurring in the data.
        /// </summary>
        private List<double> _weights;

        private int _minAreaIndex;
        private int _maxAreaIndex;
        private int _minWeightIndex;
        private int _maxWeightIndex;

        private bool _noFilteringJustHightlight;

        /// <summary>
        /// Min area for user feedback.
        /// </summary>
        public double MinArea
        {
            get
            {
                return Math.Round(_areas[_minAreaIndex], Digits);
            }
        }

        /// <summary>
        /// Max area for user feedback.
        /// </summary>
        public double MaxArea
        {
            get
            {
                return Math.Round(_areas[_maxAreaIndex], Digits);
            }
        }

        /// <summary>
        /// Min weight for user feedback.
        /// </summary>
        public double MinWeight
        {
            get
            {
                return Math.Round(_weights[_minWeightIndex], Digits);
            }
        }

       

        /// <summary>
        /// Max weighta for user feedback.
        /// </summary>
        public double MaxWeight
        {
            get
            {
                return Math.Round(_weights[_maxWeightIndex], Digits);
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

        /// <summary>
        /// If this flag is set the items are not removed when filtering but highlighted.
        /// </summary>
        public bool NoFilterJustHighlight
        {
            get => _noFilteringJustHightlight;
            set
            {
                if (_noFilteringJustHightlight != value)
                {
                    _noFilteringJustHightlight = value;

                    OnPropertyChanged();                 
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

        public string WeightSemantic { get; set; } = "Area";
        public string AreaSemantic { get; set; } = "Weight";

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