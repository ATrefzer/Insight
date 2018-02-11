using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Insight.Shared
{
    /// <summary>
    ///     Chaining of multiple filters. All filters must accept!
    /// </summary>
    public sealed class Filter : IFilter
    {
        private readonly IFilter[] _filters;

        public Filter(params IFilter[] filters)
        {
            var nonNull = filters.Where(filter => filter != null);
            _filters = nonNull.ToArray();
        }

        public bool IsAccepted(string path)
        {
            if (!_filters.Any())
            {
                return true;
            }

            return _filters.All(filter => filter.IsAccepted(path));
        }
    }

    /// <summary>
    /// Only files from a given list is accepted.
    /// </summary>
    public class FileFilter : IFilter
    {
        private readonly HashSet<string> _acceptedFiles;

        public FileFilter(List<string> acceptedFiles)
        {
            _acceptedFiles = new HashSet<string>(acceptedFiles.Select(x => x.ToLowerInvariant()));
        }

        public bool IsAccepted(string path)
        {
            return _acceptedFiles.Contains(path.ToLower());
        }
    }

    /// <summary>
    ///     Note that given extensions are allowed!
    /// </summary>
    public sealed class ExtensionIncludeFilter : IFilter
    {
        private readonly string[] _allowedExtensions;

        public ExtensionIncludeFilter(params string[] allowedExtensions)
        {
            _allowedExtensions = allowedExtensions.Select(x => x.ToLowerInvariant()).ToArray();
        }

        public bool IsAccepted(string path)
        {
            return _allowedExtensions.Any(path.ToLowerInvariant().EndsWith);
        }
    }

    public class InverseFilter : IFilter
    {
        private readonly IFilter _filter;


        public InverseFilter(IFilter filter)
        {
            _filter = filter;
        }

        public bool IsAccepted(string path)
        {
            return !_filter.IsAccepted(path);
        }
    }

    /// <summary>
    ///     Note that given paths are not allowed!
    /// </summary>
    public sealed class PathExcludeFilter : IFilter
    {
        private readonly string[] _disallowedPaths;

        public PathExcludeFilter(params string[] disallowedPaths)
        {
            _disallowedPaths = disallowedPaths.Select(path => path.ToLowerInvariant()).ToArray();
        }

        public bool IsAccepted(string path)
        {
            return !_disallowedPaths.Any(path.ToLowerInvariant().Contains);
        }
    }


    public sealed class OnlyFilesWithinRootDirectoryFilter : IFilter
    {
        private readonly string _rootDir;

        public OnlyFilesWithinRootDirectoryFilter(string rootDir)
        {
            _rootDir = rootDir;
        }

        public bool IsAccepted(string path)
        {
            return path.StartsWith(_rootDir, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    internal class OnlyUnitTests : IFilter
    {
        public bool IsAccepted(string path)
        {
            return path.Contains("UnitTest") && path.Contains("_") && path.EndsWith(".cs");
        }
    }
}