using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Visualization.Controls.Utility;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Visualization.Controls
{
    public class ToolViewModel : INotifyPropertyChanged
    {
        private double _maxArea;
        private double _maxWeight;
        private double _minArea;
        private double _minWeight;

        public ToolViewModel(Range<double> areaRange, Range<double> weightRange)
        {
            AreaRange = areaRange;
            WeightRange = weightRange;
            Reset();
        }

        public event Action FilterChanged;
        public event Action SearchPatternChanged;

        public event PropertyChangedEventHandler PropertyChanged;
     

        public Range<double> AreaRange { get; }

        public double MaxArea
        {
            get => _maxArea;
            set
            {
                if (_maxArea != value)
                {
                    _maxArea = value;
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

        public double MaxWeight
        {
            get => _maxWeight;
            set
            {
                if (_maxWeight != value)
                {
                    _maxWeight = value;
                    OnPropertyChanged();
                    OnFilterChanged();
                }
            }
        }

        public double MinArea
        {
            get => _minArea;
            set
            {
                if (_minArea != value)
                {
                    _minArea = value;
                    OnPropertyChanged();
                    OnFilterChanged();
                }
            }
        }

        public double MinWeight
        {
            get => _minWeight;
            set
            {
                if (_minWeight != value)
                {
                    _minWeight = value;
                    OnPropertyChanged();
                    OnFilterChanged();
                }
            }
        }

        public Range<double> WeightRange { get; }

        public bool IsAreaValid(double area)
        {
            return area >= MinArea && area <= MaxArea;
        }

        public bool IsWeightValid(double weight)
        {
            return weight >= MinWeight && weight <= MaxWeight;
        }

        public void Reset()
        {
            MinWeight = WeightRange.Min;
            MaxWeight = WeightRange.Max;
            MinArea = AreaRange.Min;
            MaxArea = AreaRange.Max;
        }

        protected virtual void OnFilterChanged()
        {
            FilterChanged?.Invoke();
        }

        protected virtual void OnSearchPatternChanged()
        {
            SearchPatternChanged?.Invoke();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

      
    }
}