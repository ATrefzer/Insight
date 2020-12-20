using Insight.Shared;

using Visualization.Controls.Interfaces;

namespace Insight.Dialogs
{
    public static class ColorPaletteExtensions
    {
        public static IColorScheme ForAlias(this IColorScheme palette, IAliasMapping aliasMapping)
        {
            return new AliasColorScheme(palette, aliasMapping);
        }
    }
}