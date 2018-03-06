using System.Windows.Media;

namespace Visualization.Controls.Interfaces
{
    public interface IColorScheme
    {
        SolidColorBrush GetMediaBrush(string key);
    }
}