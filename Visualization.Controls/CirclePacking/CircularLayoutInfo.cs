using System;
using System.Globalization;
using System.Windows;

using Visualization.Controls.Common;

namespace Visualization.Controls.CirclePacking
{
    [Serializable]
    public sealed class CircularLayoutInfo : LayoutInfo
    {
        public Point Center { get; set; }

        public Vector PendingChildrenMovement { get; set; }
        public double Radius { get; set; }

        public override void Backup()
        {
        }

        public override bool IsHit(Point mousePos)
        {
            var vec = Center - mousePos;
            return vec.Length < Radius;
        }

        public void Move(Vector vec)
        {
            Center += vec;
            PendingChildrenMovement += vec;
        }

        public override void Rollback()
        {
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"(x-{Center.X:0.#####})^2+(y-{Center.Y:0.#####})^2={Radius.ToString(CultureInfo.InvariantCulture)}^2");
        }
    }
}