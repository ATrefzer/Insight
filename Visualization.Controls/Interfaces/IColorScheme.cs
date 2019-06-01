using System.Windows.Media;

namespace Visualization.Controls.Interfaces
{
    public interface IColorScheme
    {      
        string GetColorName(string name);
        SolidColorBrush GetBrush(string key);
    }
}