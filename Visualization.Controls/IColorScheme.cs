using System.Windows.Media;

namespace Visualization.Controls
{
    public interface IColorScheme
    {
        SolidColorBrush GetMediaBrush(string key);
    }
}