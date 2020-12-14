using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

using Insight.Shared;

using Visualization.Controls;
using Visualization.Controls.Interfaces;

namespace Insight
{
    /// <summary>
    /// Manages the colors used in the current project.
    /// The colors shall stay stable regardless of the order of your analysis.
    /// Opening a file trend and then showing knowledge shall result in the same colors not matter in which order
    /// you execute it. Therefore a color file is written the first time we create a cache. After this the color file
    /// is never deleted, only new colors are appended. This keeps the colors the same. Plus it gives the user the option
    /// to edit this file.
    /// </summary>
    public sealed class ColorSchemeManager : IColorSchemeManager
    {
        private readonly string _pathToColorFile;
        public static string DefaultFileName = "colors.json";

        public ColorSchemeManager(string pathToColorFile)
        {
            _pathToColorFile = pathToColorFile;
        }

        public void UpdateAndSave(IColorPalette colorScheme, IEnumerable<ColorMapping> updates, IEnumerable<Color> newColors)
        {
            var colorSchemeObj = (ColorScheme)colorScheme;

            // Ensure all new colors exist even if not used in the mappings.
            foreach (var color in newColors)
            {
                colorSchemeObj.AddColor(color);
            }

            colorSchemeObj.Update(updates);
            Save(colorSchemeObj);
        }

        /// <summary>
        /// Once the color file is created it is not deleted because the user can edit it.
        /// </summary>
        public bool UpdateColorScheme(List<string> names)
        {
            var updated = false;

            var scheme = ReadColorSchemeFile();
            if (scheme == null)
            {
                // Create a new scheme
                scheme = new ColorScheme(names);
                updated = true;
            }
            else
            {
                // Add missing developers not present the time the file was created. (keep sort order)
                var missingNames = names.ToList();
                missingNames.RemoveAll(name => scheme.Names.Contains(name));

                if (missingNames.Any())
                {
                    foreach (var newName in missingNames)
                    {
                        scheme.AssignFreeColor(newName);
                    }

                    updated = true;
                }
            }

            if (updated)
            {
                Save(scheme);
            }

            return updated;
        }


        public IColorScheme LoadColorScheme()
        {
            return ReadColorSchemeFile();
        }

        public void Save(IColorScheme colorScheme)
        {
            var json = new JsonFile<ColorScheme>();
            json.Write(_pathToColorFile, (ColorScheme)colorScheme);
        }


        private ColorScheme ReadColorSchemeFile()
        {
            if (!File.Exists(_pathToColorFile))
            {
                return null;
            }

            var json = new JsonFile<ColorScheme>();
            var scheme = json.Read(_pathToColorFile);

            return scheme;
        }
    }
}