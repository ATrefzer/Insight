using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Insight.Shared.Model;

namespace Insight.Metrics
{
    internal enum State
    {
        InsideBlockComment,
        InsideLineComment,
        SomewhereElse
    }

    /// <summary>
    /// Inverted space is a metric for code readability or the complexity of the file.
    /// </summary>
    internal sealed class InvertedSpaceMetric
    {
        public InvertedSpace CalculateInvertedSpaceMetric(FileInfo file)
        {
            var logicalSpacesByLine = File.ReadLines(file.FullName)
                .Where(IsNonEmptyLine)
                .Select(line => GetLogicalSpaces(line));

            return CalculateStatistics(logicalSpacesByLine);
        }

        /// <summary>
        /// Removes all comments from the given file content.
        /// </summary>
        public string StripComments(string fileContent)
        {
            var state = State.SomewhereElse;

            var builder = new StringBuilder(fileContent.Length);
            for (var index = 0; index < fileContent.Length; index++)
            {
                var c = fileContent[index];
                var peek = GetPeek(fileContent, index);

                if (state == State.SomewhereElse && c == '/')
                {
                    if (peek == '/')
                        state = State.InsideLineComment;
                    else if (peek == '*')
                        state = State.InsideBlockComment;
                    else
                        builder.Append(c);
                }

                else if (state == State.InsideLineComment && (c == '\n' || c == '\r'))
                {
                    // Single line comment ends with line end (or file end)
                    state = State.SomewhereElse;

                    // For any other character we continue skipping the command character
                }
                else if (state == State.InsideBlockComment && c == '*' && peek == '/')
                {
                    // Block comment ends with */ (or end of file)
                    state = State.SomewhereElse;
                }
                else
                {
                    // Forgot any case above to handle=
                    Debug.Assert(false);
                }
            }

            return builder.ToString();
        }


        private static char GetPeek(string fileContent, int index)
        {
            return index == fileContent.Length ? (char) 0 : fileContent[index + 1];
        }

        private InvertedSpace CalculateStatistics(IEnumerable<int> logicalSpacesByLine)
        {
            var data = logicalSpacesByLine.ToArray();

            var min = data.Min();
            var max = data.Max();
            var mean = Statistics.Mean(data);
            var sd = Statistics.StandardDeviation(data);
            var total = data.Sum();

            return new InvertedSpace
            {
                Min = min,
                Max = max,
                Mean = mean,
                StandardDeviation = sd,
                Total = total
            };
        }

        /// <summary>
        /// LogicalSpace is the number of spaces that represent one indentation.
        /// </summary>
        private int GetLogicalSpaces(string line, int spacesPerLogicalSpace = 4)
        {
            var spaces = 0;
            var tabs = 0;

            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ')
                {
                    spaces++;
                }
                else if (line[i] == '\t')
                {
                    tabs++;
                }
                else
                {
                    // Count until the first non whitespace is hit
                    break;
                }
            }

            return tabs + spaces / spacesPerLogicalSpace;
        }

        private bool IsNonEmptyLine(string line)
        {
            // Regex for empty line
            return !Regex.IsMatch(line, @"^\s*$");
        }
    }
}