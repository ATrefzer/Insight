using System;
using System.Collections.Generic;
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

        public FileFilter(IEnumerable<string> acceptedFiles)
        {
            _acceptedFiles = new HashSet<string>(acceptedFiles.Select(x => x.ToLowerInvariant()));
        }

        public bool IsAccepted(string path)
        {
            var accepted = _acceptedFiles.Contains(path.ToLowerInvariant());
            return accepted;
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
            var accepted = _allowedExtensions.Any(path.ToLowerInvariant().EndsWith);
            return accepted;
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
            var accept = _filter.IsAccepted(path);
            return !accept;
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
            // One disallowed path is enough to reject.
            var reject = _disallowedPaths.Any(path.ToLowerInvariant().Contains);
            var accepted = !reject;
            return accepted;
        }
    }

    /// <summary>
    ///     Note that given paths are allowed!
    /// </summary>
    public sealed class PathIncludeFilter : IFilter
    {
        private readonly string[] _expectedPaths;

        public PathIncludeFilter(params string[] expectedPaths)
        {
            _expectedPaths = expectedPaths.Select(path => path.ToLowerInvariant()).ToArray();
        }

        public bool IsAccepted(string path)
        {
            // One expected path is enough to accept.
            var accept = _expectedPaths.Any(path.ToLowerInvariant().Contains);
            return accept;
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
            var accept = path.StartsWith(_rootDir, StringComparison.InvariantCultureIgnoreCase);
            return accept;
        }
    }

    internal class OnlyUnitTests : IFilter
    {
        public bool IsAccepted(string path)
        {
            var accepted = path.Contains("UnitTest") && path.Contains("_") && path.EndsWith(".cs");
            return accepted;
        }
    }
}