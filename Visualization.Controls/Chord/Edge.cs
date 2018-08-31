using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;

namespace Visualization.Controls.Chord
{
    public class Edge : IChordElement, INotifyPropertyChanged
    {
        private bool _isSelected;

        private Point _point1;
        private Point _point2;
        private Point _point3;
        private double strength;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }


        public string Name { get; set; }
        public string Node1Id { get; set; }
        public string Node2Id { get; set; }

        public Point Point1
        {
            get => _point1;

            set
            {
                _point1 = value;
                OnPropertyChanged();
            }
        }

        public Point Point2
        {
            get => _point2;

            set
            {
                _point2 = value;
                OnPropertyChanged();
            }
        }

        public Point Point3
        {
            get => _point3;

            set
            {
                _point3 = value;
                OnPropertyChanged();
            }
        }

        public double Strength
        {
            get => strength;

            set
            {
                strength = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectCommand { get; internal set; }

        internal void UpdateLocation(Dictionary<string, Vertex> vertexLookup)
        {
            var vertex1 = vertexLookup[Node1Id];
            var vertex2 = vertexLookup[Node2Id];

            Point1 = vertex1.Center;

            // Center of circle
            Point2 = new Point();
            Point3 = vertex2.Center;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}