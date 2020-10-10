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
        string Cache { get; }
        IFilter Filter { get; set; }
        string SourceControlDirectory { get; set; }

        bool IsDefault { get; }
        bool IsValid();
        void Load(string path);
        ISourceControlProvider CreateProvider();
        IEnumerable<string> GetNormalizedFileExtensions();
    }


    [Serializable]
    public sealed class Project : IProject
    {
        private string _extensionsToInclude = "";
        private string _pathToExclude = "";
        private string _pathToInclude;

        private string _sourceControlDirectory;

        public Project()
        {
            InitNewProject();
            IsDefault = true;
        }

        public string Cache => GetCacheDirectory();

        public bool IsDefault { get; set; }


        public string ExtensionsToInclude
        {
            get => _extensionsToInclude;
            set
            {
                _extensionsToInclude = value;
                UpdateFilter();
            }
        }

        public string ProjectName { get; set; }

        /// <summary>
        /// Summary filter: Which files from the history are considered for the analysis.
        /// </summary>
        [XmlIgnore]
        public IFilter Filter { get; set; }

        /// <summary>
        /// Need one to reject
        /// </summary>
        public string PathsToExclude
        {
            get => _pathToExclude;
            set
            {
                _pathToExclude = value;
                UpdateFilter();
            }
        }

        /// <summary>
        /// Need one to accept
        /// </summary>
        public string PathsToInclude
        {
            get => _pathToInclude;
            set
            {
                _pathToInclude = value;
                UpdateFilter();
            }
        }

        public string SourceControlDirectory
        {
            get => _sourceControlDirectory;
            set
            {
                _sourceControlDirectory = value;

                // Project base is used in filters!
                UpdateFilter();
            }
        }

        public string Provider { get; set; }

        [XmlIgnore] public ITeamClassifier TeamClassifier { get; set; }

        public string WorkItemRegEx { get; set; }


        public string ProjectParentDirectory { get; set; }

        public ISourceControlProvider CreateProvider()
        {
            var type = Type.GetType(Provider);
            if (type == null)
            {
                throw new ArgumentException(Provider);
            }

            var provider = Activator.CreateInstance(type) as ISourceControlProvider;
            if (provider == null)
            {
                throw new Exception($"Failed creating '{type}'");
            }

            provider.Initialize(SourceControlDirectory, Cache, Filter, WorkItemRegEx);
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

        public bool IsSourceControlDirectoryValid()
        {
            return Directory.Exists(SourceControlDirectory);
        }

        public bool IsValid()
        {
            return IsSourceControlDirectoryValid() && IsCacheValid() && IsProjectDirectoryValid();
        }

        public string GetProjectFile()
        {
            return Path.Combine(ProjectParentDirectory, ProjectName, "insight.project");
        }

        public string GetProjectDirectory()
        {
            return Path.Combine(ProjectParentDirectory, ProjectName);
        }

        public bool IsProjectDirectoryValid()
        {
            return Directory.Exists(GetProjectDirectory());
        }

        public void Load(string path)
        {
            var file = new XmlFile<Project>();
            var tmp = file.Read(path);

            SourceControlDirectory = tmp.SourceControlDirectory;
            ProjectParentDirectory = tmp.ProjectParentDirectory;
            ProjectName = tmp.ProjectName;
            ExtensionsToInclude = tmp.ExtensionsToInclude;
            PathsToExclude = tmp.PathsToExclude;
            PathsToInclude = tmp.PathsToInclude;
            Provider = tmp.Provider;
            WorkItemRegEx = tmp.WorkItemRegEx;
            TeamClassifier = tmp.TeamClassifier;
            IsDefault = false;

            UpdateFilter();
        }

        public void Save()
        {
            var file = GetProjectFile();
            SaveTo(file);
            IsDefault = false;
        }

        public void SaveTo(string path)
        {
            var file = new XmlFile<Project>();
            file.Write(path, this);
        }


        public void InitProjectEnvironment()
        {
            if (IsSourceControlDirectoryValid() is false)
            {
                throw new Exception(Strings.SourceControlDirectoyNotFound);
            }

            if (Directory.Exists(GetProjectDirectory()))
            {
                Directory.Delete(GetProjectDirectory(), true);
            }

            Directory.CreateDirectory(GetProjectDirectory());

            if (Directory.Exists(GetCacheDirectory()) is false)
            {
                Directory.CreateDirectory(GetCacheDirectory());
            }
        }

        public void LoadFromDirectory(string directory)
        {
            var file = Path.Combine(directory, "insight.project");
            Load(file);
        }

        private string GetCacheDirectory()
        {
            return Path.Combine(GetProjectDirectory(), "Cache");
        }


        private void InitNewProject()
        {
            ProjectParentDirectory = ".\\Project-Parent-Directory";
            SourceControlDirectory = ".\\Source-Control-Directory";
            ProjectName = "Project-Name";
            ExtensionsToInclude = Settings.Default.ExtensionsToInclude.Trim();
            PathsToExclude = Settings.Default.PathsToExclude.Trim();
            PathsToInclude = Settings.Default.PathsToInclude.Trim();
            Provider = Settings.Default.Provider.Trim();
            WorkItemRegEx = Settings.Default.WorkItemRegEx.Trim();
            TeamClassifier = default(ITeamClassifier);
            UpdateFilter();
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

            if (!string.IsNullOrEmpty(PathsToInclude))
            {
                var paths = SplitTrimAndToLower(PathsToInclude);
                filters.Add(new PathIncludeFilter(paths.ToArray()));
            }

            // Remvove all files that are not in the base directory.
            // (History / log may return files outside)
            filters.Add(new OnlyFilesWithinRootDirectoryFilter(SourceControlDirectory));

            // All filters must apply
            Filter = new Filter(filters.ToArray());
        }
    }
}