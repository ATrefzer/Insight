using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Visualization.Controls.Chord
{
    internal class Vertex : IChordElement, INotifyPropertyChanged
    {
        public string _nodeId;
        private Point _center;

        private bool _isSelected;

        public Vertex(string nodeId, string name)
        {
            Name = name;
            NodeId = nodeId;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public double Angle { get; internal set; }

        public Point Center
        {
            get => _center;

            set
            {
                _center = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Name { get; }

        public string NodeId
        {
            get => _nodeId;
            set
            {
                _nodeId = value;
                OnPropertyChanged();
            }
        }

        public double Radius { get; set; }
        public ICommand SelectCommand { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Vertex) obj);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        public void UpdateLocation(double radiusOfMainCircle)
        {
            var x = radiusOfMainCircle * Math.Cos(Angle);
            var y = radiusOfMainCircle * Math.Sin(Angle);
            Center = new Point(x, y);
        }

        protected bool Equals(Vertex other)
        {
            return string.Equals(Name, other.Name);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}