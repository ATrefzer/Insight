using System.Collections.Generic;

using Visualization.Controls;
using Visualization.Controls.Interfaces;

namespace Insight
{
    public interface IColorSchemeManager
    {
        /// <summary>
        /// Once the color file is created it is not deleted because the user can edit it.
        /// Assume the names are ordered such that the most relevant entries come first.
        /// </summary>
        bool UpdateColorScheme(List<string> orderedNames);

        /// <summary>
        /// Updates color assignments done by the user.
        /// </summary>
        void UpdateAndSave(IColorScheme colorScheme, List<ColorMapping> updates);

        IColorScheme LoadColorScheme();
        
    }
}