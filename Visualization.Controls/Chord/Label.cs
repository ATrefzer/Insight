using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

using Prism.Commands;

namespace Visualization.Controls.Chord
{
    /// <summary>
    /// Notes about scaling
    /// - The origin for the TextBox is the upper left corner.
    /// - In Q1 and Q4 we scale Y-axis by -1. This way we undo the scaling of the canvas
    ///   and the text is no longer upside down. Origin of the TextBox is the upper left corner.
    /// - In Q2 and Q4 the text is - by the rotation - already upside down and reverses
    ///   the canvas' scaling. The user can read the text but the direction is inverted
    ///   (by the rotation). Therefore we scale the X-axis by -1. But now the text flows inside
    ///   the circle.
    /// Notes about adjusting the TextBox origin
    /// </summary>
    internal class Label : IChordElement, INotifyPropertyChanged
    {
        private readonly double _labelHeight;
        private readonly double _labelWidth;

        private string _text;

        private Point _location;

        /// <summary>
        /// If there is no space for the labels they are collapsed.
        /// </summary>
        private Visibility _visibility = Visibility.Visible;


        public Label(string text, double angleInRad, Size size)
        {
            Angle = angleInRad;

            // wpf counts clockwise
            AngleInDegrees = -(Angle * 180.0 / Math.PI);

            _text = text;
            _labelWidth = size.Width;
            _labelHeight = size.Height;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public double Angle { get; }

        public double AngleInDegrees { get; set; }

        public Visibility IsVisible
        {
            get => _visibility;
            set
            {
                if (value == _visibility)
                {
                    return;
                }

                _visibility = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _selected;

        public bool IsSelected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    OnPropertyChanged();
                }
            }
        }

        public Point Location
        {
            get => _location;
            set
            {
                if (_location != value)
                {
                    _location = value;
                    OnPropertyChanged();
                }
            }
        }

        public double XScale { get; set; }
        public double YScale { get; set; }
        public DelegateCommand MouseEnterCommand { get; internal set; }
        public DelegateCommand MouseLeaveCommand { get; internal set; }
       

        public static Vector PerpendicularClockwise(Vector vector2)
        {
            return new Vector(-vector2.Y, vector2.X);
        }

        public static Vector PerpendicularCounterClockwise(Vector vector2)
        {
            return new Vector(vector2.Y, -vector2.X);
        }

        public void UpdateLocation(double radiusOfMainCircle)
        {
            const double padding = 12.0;

            // Vertex
            var x = radiusOfMainCircle * Math.Cos(Angle);
            var y = radiusOfMainCircle * Math.Sin(Angle);
            var vertex = new Point(x, y);

            var originToVertex = (Vector) new Point(x, y);

            if (IsQ1OrQ4())
            {
                XScale = 1;
                YScale = -1;

                // Move textbox to the outside such that a padding is between the left edge and the circle.
                var originToTextBox = MoveTextBoxOriginAwayFromCenter(vertex, padding);

                // Move origin of the TextBox along the tangent line such that it is aligned with the vertex.
                var tangent = PerpendicularClockwise(originToVertex);
                tangent.Normalize();
                var displacement = tangent * _labelHeight / 2.0;

                Location = (Point) (originToTextBox + displacement);
            }
            else if (IsQ2OrQ3())
            {
                XScale = -1;
                YScale = 1;

                // Move TextBox to the outside such that its right edge touches the circle.
                var originToTextBox = MoveTextBoxOriginAwayFromCenter(vertex, _labelWidth + padding);

                // Move origin of the TextBox along the tangent line
                // such that it is aligned with the vertex.
                var tangent = PerpendicularCounterClockwise(originToVertex);
                tangent.Normalize();
                var displacement = tangent * _labelHeight / 2.0;

                Location = (Point) (originToTextBox + displacement);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool IsQ1OrQ4()
        {
            return Angle >= Math.PI * 3.0 / 2.0 || Angle >= 0 && Angle <= Math.PI / 2.0;
        }

        private bool IsQ2OrQ3()
        {
            return Angle > Math.PI / 2.0 && Angle < Math.PI * 3.0 / 2.0;
        }

        private Vector MoveTextBoxOriginAwayFromCenter(Point vertex, double units)
        {
            var originToTextBox = (Vector) vertex;
            var newLength = originToTextBox.Length + units;
            originToTextBox.Normalize();
            originToTextBox = originToTextBox * newLength;
            return originToTextBox;
        }
    }
}