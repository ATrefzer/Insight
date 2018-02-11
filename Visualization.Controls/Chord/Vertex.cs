using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Visualization.Controls.Chord
{
    class Vertex : IChordElement, INotifyPropertyChanged
    {
        public Vertex(string name)
        {
            Name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool Equals(Vertex other)
        {
            return String.Equals(Name, other.Name);
        }

        bool _isSelected;

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }

            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((Vertex) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public string Name { get; set; }
        Point _center;

        public Point Center
        {
            get
            {
                return _center;
            }

            set
            {
                _center = value;
                OnPropertyChanged();
            }
        }

        public double Radius { get; set; }
        public double Angle { get; internal set; }
        public ICommand SelectCommand { get; set; }
        public void UpdateLocation(double radiusOfMainCircle)
        {
            var x = radiusOfMainCircle * Math.Cos(Angle);
            var y = radiusOfMainCircle * Math.Sin(Angle);
            Center = new Point(x, y);
        }
    }
}