using System.Collections.Generic;
using System.IO;

namespace Insight.Metrics
{
    /// <summary>
    /// Interface for the Insight.Metrics assembly.
    /// </summary>
    public interface IMetricProvider
    {
        /// <summary>
        /// Calculates the inverted space code metric for a single given file.
        /// </summary>
        InvertedSpace CalculateInvertedSpaceMetric(FileInfo file);

        /// <summary>
        /// Calculates the lines of code metric for a single given file.
        /// </summary>
        LinesOfCode CalculateLinesOfCode(FileInfo file);

        /// <summary>
        /// Calculates the lines of code metric for files in the given directory.
        /// If a file is processed depends on its file extension.
        /// Normalized file extensions means: Lower case, including the dot.
        /// </summary>
        Dictionary<string, LinesOfCode> CalculateLinesOfCode(System.IO.DirectoryInfo rootDir, IEnumerable<string> normalizedFileExtensions);

        /// <summary>
        /// Reads the cached metric file. <see cref="UpdateLinesOfCodeCache" />.
        /// Returns a mapping from full file path to lines of code metric.
        /// Throws a FileNotFoundException if the cache file does not exist.
        /// </summary>
        Dictionary<string, LinesOfCode> QueryCachedLinesOfCode(string cacheDirectory);

        /// <summary>
        /// Rebuilds the metric cache file.
        /// </summary>
        void UpdateLinesOfCodeCache(string startDirectory, string cacheDirectory, IEnumerable<string> normalizedFileExtensions);
    }
}