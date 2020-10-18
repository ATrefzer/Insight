using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    public class ColorSchemeManager
    {
        public string DefaultFileName = "colors.json";

        /// <summary>
        /// Once the color file is created it is not deleted because the user can edit it.
        /// Assume the names are ordered such that the most relevant entries come first.
        /// </summary>
        public bool UpdateColorScheme(string pathToColorFile, List<string> orderedNames)
        {
            var updated = false;

            var scheme = ReadColorSchemeFile(pathToColorFile);
            if (scheme == null)
            {
                // Create a new scheme
                scheme = new ColorScheme(orderedNames.ToArray());
                updated = true;
            }
            else
            {
                // Add missing developers not present the time the file was created. (keep sort order)
                var missingNames = orderedNames.ToList();
                missingNames.RemoveAll(name => scheme.Names.Contains(name));

                if (missingNames.Any())
                {
                    foreach (var newName in missingNames)
                    {
                        scheme.AddColorFor(newName);
                    }

                    updated = true;
                }
            }

            if (updated)
            {
                var json = new JsonFile<ColorScheme>();
                json.Write(pathToColorFile, scheme);
            }

            return updated;
        }


        internal IColorScheme GetColorScheme(string pathToColorFile)
        {
            return ReadColorSchemeFile(pathToColorFile);
        }

        private ColorScheme ReadColorSchemeFile(string pathToColorFile)
        {
            if (!File.Exists(pathToColorFile))
            {
                return null;
            }

            var json = new JsonFile<ColorScheme>();
            var scheme = json.Read(pathToColorFile);

            return scheme;
        }
    }
}