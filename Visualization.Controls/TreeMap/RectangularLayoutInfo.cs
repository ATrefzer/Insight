using System;
using System.Windows;

namespace Visualization.Controls.TreeMap
{
    [Serializable]
    public abstract class LayoutInfo
    {
        public abstract void Backup();
        public abstract bool IsHit(Point mousePos);
        public abstract void Rollback();
    }

    [Serializable]
    public sealed class RectangularLayoutInfo : LayoutInfo
    {
        public Rect Rect;
        private Rect _backup;


        public override void Backup()
        {
            _backup = Rect;
        }


        public override bool IsHit(Point mousePos)
        {
            return Rect.Contains(mousePos);
        }

        public override void Rollback()
        {
            // Copy, no reference.
            Rect = _backup;
        }
    }
}