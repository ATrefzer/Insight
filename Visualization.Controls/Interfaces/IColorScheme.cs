using System.Collections.Generic;
using System.Windows.Media;

namespace Visualization.Controls.Interfaces
{
    public interface IColorScheme : IEnumerable<ColorMapping>
    {      
        string GetColorName(string name);
        SolidColorBrush GetBrush(string name);
    }
}