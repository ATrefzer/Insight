using System.Windows.Media;

namespace Visualization.Controls
{
    public interface IColorScheme
    {
        SolidColorBrush Highlight { get; }
        SolidColorBrush GetBrush(string key);
    }
}