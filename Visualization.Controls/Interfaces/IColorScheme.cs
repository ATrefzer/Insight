using System.Windows.Media;

namespace Visualization.Controls.Interfaces
{
    public interface IColorScheme
    {      
        System.Drawing.Brush GetBrush(string key);
        string GetColorName(string name);
        SolidColorBrush GetMediaBrush(string key);
    }
}