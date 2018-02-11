using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using Insight.Properties;
using Insight.Shared;

namespace Insight
{
    public interface IProject
    {
    }

    [Serializable]
    public sealed class Project
    {
        private string _extensionsToInclude = "";
        private string _pathToExclude = "";

        private string _projectBase;
        public string Cache { get; set; }

        public string ExtensionsToInclude
        {
            get => _extensionsToInclude;
            set
            {
                _extensionsToInclude = value;
                UpdateFilter();
            }
        }

        /// <summary>
        /// Summary filter: Which files from the history are considered for the analysis.
        /// </summary>
        [XmlIgnore]
        public Filter Filter { get; set; }

        public string PathsToExclude
        {
            get => _pathToExclude;
            set
            {
                _pathToExclude = value;
                UpdateFilter();
            }
        }

        public string ProjectBase
        {
            get => _projectBase;
            set
            {
                _projectBase = value;

                // Project base is used in filters!
                UpdateFilter();
            }
        }

        public string Provider { get; set; }

        [XmlIgnore]
        public ITeamClassifier TeamClassifier { get; set; }

        public string WorkItemRegEx { get; set; }

        public ISourceControlProvider CreateProvider()
        {
            var type = Type.GetType(Provider);
            if (type == null)
            {
                throw new ArgumentException(Provider);
            }

            var provider = Activator.CreateInstance(type, ProjectBase, Cache, WorkItemRegEx) as ISourceControlProvider;
            return provider;
        }

        /// <summary>
        /// For example Xml -> .xml
        /// </summary>
        public IEnumerable<string> GetNormalizedFileExtensions()
        {
            var all = new List<string>();
            var parts = SplitTrimAndToLower(ExtensionsToInclude);

            foreach (var extension in parts)
            {
                var normalized = extension;

                // cut off dot if it exists
                var dot = normalized.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);
                if (dot >= 0)
                {
                    normalized = normalized.Substring(dot + 1);
                }

                all.Add("." + normalized);
            }

            return all.Distinct();
        }

        public bool IsCacheValid()
        {
            return Directory.Exists(Cache);
        }

        public bool IsProjectBaseValid()
        {
            return Directory.Exists(ProjectBase);
        }

        public bool IsValid()
        {
            return IsProjectBaseValid() && IsCacheValid();
        }

        public void Load()
        {
            ProjectBase = Settings.Default.ProjectBase.Trim();
            Cache = Settings.Default.Cache.Trim();
            ExtensionsToInclude = Settings.Default.ExtensionsToInclude.Trim();
            PathsToExclude = Settings.Default.PathsToExclude.Trim();
            Provider = Settings.Default.Provider.Trim();
            WorkItemRegEx = Settings.Default.WorkItemRegEx.Trim();
            TeamClassifier = default(ITeamClassifier);
            UpdateFilter();
        }

        public void LoadFrom(string path)
        {
            var file = new XmlFile<Project>();
            var tmp = file.Read(path);

            Cache = tmp.Cache;
            ProjectBase = tmp.ProjectBase;
            PathsToExclude = tmp.PathsToExclude;
            ExtensionsToInclude = tmp.ExtensionsToInclude;
            Provider = tmp.Provider;
            WorkItemRegEx = tmp.WorkItemRegEx;
            TeamClassifier = tmp.TeamClassifier;
        }

        public void Save()
        {
            Settings.Default.ProjectBase = ProjectBase;
            Settings.Default.Cache = Cache;
            Settings.Default.PathsToExclude = PathsToExclude;
            Settings.Default.ExtensionsToInclude = ExtensionsToInclude;
            Settings.Default.Provider = Provider;
            Settings.Default.WorkItemRegEx = WorkItemRegEx;
            Settings.Default.Save();
        }

        public void SaveTo(string path)
        {
            var file = new XmlFile<Project>();
            file.Write(path, this);
        }

        /// <summary>
        /// Split, Trim, and ToLower
        /// </summary>
        private IEnumerable<string> SplitTrimAndToLower(string splitThis)
        {
            if (string.IsNullOrEmpty(splitThis))
            {
                return Enumerable.Empty<string>();
            }

            var splitChars = new[] { ',', ';' };
            var parts = splitThis.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => x.Trim().ToLowerInvariant());

            return parts;
        }

        private void UpdateFilter()
        {
            // Filters to make the file summary

            var filters = new List<IFilter>();
            if (!string.IsNullOrEmpty(ExtensionsToInclude))
            {
                var extensions = GetNormalizedFileExtensions();
                filters.Add(new ExtensionIncludeFilter(extensions.ToArray()));
            }

            if (!string.IsNullOrEmpty(PathsToExclude))
            {
                var paths = SplitTrimAndToLower(PathsToExclude);
                filters.Add(new PathExcludeFilter(paths.ToArray()));
            }

            // Remvove all files that are not in the base directory.
            // (History / log may return files outside)
            filters.Add(new OnlyFilesWithinRootDirectoryFilter(ProjectBase));

            // All filters must apply
            Filter = new Filter(filters.ToArray());
        }
    }
}