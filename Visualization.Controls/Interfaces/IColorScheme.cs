using System.Windows.Media;

namespace Visualization.Controls.Interfaces
{
    public interface IColorScheme
    {
        // TODO atr Remove this
        void AddColorKey(string name);
        System.Drawing.Brush GetBrush(string key);
        SolidColorBrush GetMediaBrush(string key);
    }
}