using Insight.Shared.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Insight.Metrics
{
    /// <summary>
    /// Calculates the lines of code for a single file or a directory using the cloc utility.
    /// See https://github.com/AlDanial/cloc/releases/tag/v1.76
    /// </summary>
    internal class LinesOfCodeMetric
    {
        /// <summary>
        /// Maps file extension -> language.
        /// The user provides file extension to consider. However to parse the cloc output we need the language.
        /// </summary>
        private readonly Dictionary<string, string> _extensionToLanguage = new Dictionary<string, string>();

        private readonly string _pathToCloc;

        public LinesOfCodeMetric(string pathToCloc)
        {
            _pathToCloc = pathToCloc;
            // These file we do support for metric calculation.

            // Cloc sometimes confuses cs files with smalltalk files. As a workaround I include smalltalk.
            _extensionToLanguage.Add(".cs", "C#,Smalltalk");
            _extensionToLanguage.Add(".xml", "XML");
            _extensionToLanguage.Add(".xaml", "XAML");

            _extensionToLanguage.Add(".java", "Java");
            _extensionToLanguage.Add(".css", "CSS");
            _extensionToLanguage.Add(".cpp", "C++");
            _extensionToLanguage.Add(".h", "C/C++ Header");
            _extensionToLanguage.Add(".c", "C");
            _extensionToLanguage.Add(".py", "Python");
        }

        private ProcessRunner CreateRunner()
        {
            return new ProcessRunner();
        }


        private string CallClocForDirectory(DirectoryInfo startDirectory, IEnumerable<string> languagesToParse)
        {
            var languages = string.Join(",", languagesToParse);

            // --skip-uniqueness If cloc finds duplicate files it skips the duplicates. We have to disable this behavior.
            var args =
                $"\"{startDirectory.FullName}\" --by-file --csv --quiet --skip-uniqueness --include-lang=\"{languages}\"";

            var runner = CreateRunner();
            var result = runner.RunProcess(_pathToCloc, args, startDirectory.FullName);
            return result.StdOut;
        }

        private string CallClocForSingleFile(FileSystemInfo file)
        {
            var args = $"\"{file.FullName}\" --csv --quiet";

            var runner = CreateRunner();
            var result = runner.RunProcess(_pathToCloc, args);
            return result.StdOut;
        }

        /// <summary>
        ///     Normalized file extensions: Lower case, including the dot.
        /// </summary>
        public Dictionary<string, LinesOfCode> CalculateLinesOfCode(DirectoryInfo startDirectory,
            IEnumerable<string> normalizedFileExtensions)
        {
            // Map file extension to language name understood by cloc
            var languagesToParse = MapFileExtensionToLanguage(normalizedFileExtensions.ToList());
            var stdOut = CallClocForDirectory(startDirectory, languagesToParse);

            return ParseClocOutput(stdOut);
        }


        private IEnumerable<string> MapFileExtensionToLanguage(IReadOnlyCollection<string> fileExtensionsWithDot)
        {
            var unknownExtensions =
                fileExtensionsWithDot.Where(ext => !_extensionToLanguage.ContainsKey(ext)).ToList();
            if (unknownExtensions.Any())
            {
                var details = string.Join(",", unknownExtensions);
                throw new ArgumentException(
                    $"Language not supported by cloc: {details} Please check the project settings.");
            }

            // Call ToArray such that the KeyNotFoundException is thrown now!
            return fileExtensionsWithDot.Select(x => _extensionToLanguage[x]).ToArray();
        }


        public LinesOfCode CalculateLinesOfCode(FileInfo file)
        {
            // Get path of this assembly
            var stdOut = CallClocForSingleFile(file);

            var dict = ParseClocOutput(stdOut);

            // 2nd is "sum"
            Debug.Assert(dict.Count == 2);
            return dict.First().Value;
        }


        private Dictionary<string, LinesOfCode> ParseClocOutput(string clocOutput)
        {
            var metrics = new Dictionary<string, LinesOfCode>();
            var lines = clocOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Structure of output file
                // language,filename,blank,comment,code

                var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 5)
                {
                    // Note there is a summary line "SUM" at the end with 4 entries.
                    continue;
                }

                
                var file = parts[1].Trim();
                var metric = CreateMetric(parts);
                metrics[file.ToLowerInvariant()] = metric;
            }

            return metrics;
        }


        private LinesOfCode CreateMetric(string[] parts)
        {
            var blank = parts[2].Trim();
            var comment = parts[3].Trim();
            var code = parts[4].Trim();

            var metric = new LinesOfCode
            {
                Code = int.Parse(code),
                Blanks = int.Parse(blank),
                Comments = int.Parse(comment)
            };
            return metric;
        }
    }
}