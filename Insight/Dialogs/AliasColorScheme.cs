using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

using Insight.Shared;
using Visualization.Controls.Common;
using Visualization.Controls.Interfaces;


namespace Insight.Dialogs
{
    /// <summary>
    /// A wrapper around a color palette. It resolves the alias first before requesting the color.
    /// </summary>
    internal sealed class AliasColorScheme : IColorScheme
    {
        private readonly IColorPalette _sourceColorPalette;
        private readonly IAliasMapping _aliasMapping;
        private Dictionary<string, ColorMapping> _aliasToColorMapping;


        public AliasColorScheme(IColorPalette sourceColorPalette, IAliasMapping aliasMapping)
        {
            _sourceColorPalette = sourceColorPalette;
            _aliasMapping = aliasMapping;

            InitAliasColorMappings();
        }

        private void InitAliasColorMappings()
        {
            if (_aliasToColorMapping != null)
            {
                return;
            }

            _aliasToColorMapping = new Dictionary<string, ColorMapping>();

            var allMappings = _sourceColorPalette.GetColorMappings().ToList();
            var nameToColor = allMappings.ToDictionary(m => m.Name, m => m.Color);

            // Convert to alias names
            foreach (var mapping in allMappings)
            {
                var name = mapping.Name;
                var alias = _aliasMapping.GetAlias(name);
                var color = mapping.Color;

                if (_aliasToColorMapping.ContainsKey(alias))
                {
                    // We may map more than one name to the same alias.
                    continue;
                }

                if (nameToColor.ContainsKey(alias))
                {
                    // This alias is an existing developer name, so we use the color instead.
                    // Otherwise we take the color of the first developer mapped to this alias
                    // by default.
                    color = nameToColor[alias];
                }

                _aliasToColorMapping.Add(alias, new ColorMapping { Name = alias, Color = color });
            }
        }

        public IEnumerable<ColorMapping> GetColorMappings()
        {
            InitAliasColorMappings();
            return _aliasToColorMapping.Values.OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Update the user made changes from the Color Editor.
        /// </summary>
        public void Update(IEnumerable<ColorMapping> aliasUpdates)
        {
            var updates = new List<ColorMapping>();
            foreach (var mapping in aliasUpdates)
            {
                var alias = mapping.Name;
                var names = _aliasMapping.GetReverse(alias).ToList();

                // If the alias itself is a developer name add it to the list, too
                if (_sourceColorPalette.IsKnown(alias))
                {
                    // x -> a
                    // a is a developer name without mapping (so it is not contained in _aliasMapping)

                    names.Add(alias);
                }

                foreach (var name in names)
                {
                    // We may multiply the color mappings here because many developers may share the same alias.
                    updates.Add(new ColorMapping { Name = name, Color = mapping.Color });
                }
            }

            _sourceColorPalette.Update(updates);
            _aliasToColorMapping = null;
        }

        public bool AddColor(Color newColor)
        {
            return _sourceColorPalette.AddColor(newColor);
        }

        public IEnumerable<Color> GetAllColors()
        {
            return _sourceColorPalette.GetAllColors();
        }

        public bool IsKnown(string alias)
        {
            return _aliasToColorMapping.ContainsKey(alias);
        }

        public SolidColorBrush GetBrush(string name)
        {
            InitAliasColorMappings();

            if (string.IsNullOrEmpty(name))
            {
                // Default color for example the unused space in knowledge loss
                return DefaultDrawingPrimitives.DefaultBrush;
            }

            if (_aliasToColorMapping.ContainsKey(name) is false)
            {
                // A developer name is not known! This should not happen.
                Debug.WriteLine($"No color mapping found for developer {name}");
                return Brushes.Black;
            }
            var mapping = _aliasToColorMapping[name];
            return BrushCache.GetBrush(mapping.Color);
        }
    }
}