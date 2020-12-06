using System.Collections.Generic;
using System.Windows.Media;

namespace Visualization.Controls.Interfaces
{
    public interface IColorScheme : IEnumerable<ColorMapping>
    {      
        string GetColorName(string name);
        SolidColorBrush GetBrush(string name);

        /// <summary>
        /// Returns all available colors. These are a bunch of default colors plus the user defined colors.
        /// </summary>
        IEnumerable<Color> GetAllColors();

        /// <summary>
        /// Returns all color mappings {name -> color}. The mappings can be updated
        /// </summary>
        IEnumerable<ColorMapping> GetColorMappings();

        /// <summary>
        /// Adds a new color to the color schema. The new color can be assigned to an existing name.
        /// </summary>
        bool AddColor(Color newColor);
    }
}