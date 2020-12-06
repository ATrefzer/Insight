using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Visualization.Controls.Chord
{
    internal class MainCircle : IChordElement, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double _radius;

        public double Radius
        {
            get
            {
                return _radius;
            }

            set
            {
                _radius = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected { get; set; } = false;
      

    }
}