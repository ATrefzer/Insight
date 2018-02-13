using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

using Insight.WpfCore;

using Prism.Commands;

namespace Insight
{
    internal sealed class ProjectViewModel : CachedModelWrapper<Project>
    {
        private readonly Dialogs _dialogs;

        public ProjectViewModel(Project project, Dialogs dialogs) : base(project)
        {
            _dialogs = dialogs;
            UpdatAll();
        }

        public ICommand ApplyCommand => new DelegateCommand<Window>(ApplySettings);

        public List<ProviderDescription> AvailableProviders
        {
            get
            {
                var svn = new ProviderDescription
                          {
                                  Class = SvnProvider.SvnProvider.GetClass(),
                                  Name = "Svn"
                          };

                var git = new ProviderDescription
                {
                    Class = GitProvider.GitProvider.GetClass(),
                    Name = "Git - DO NOT USE"
                };

                return new List<ProviderDescription>
                       {
                               svn
                               ,git
                       };
            }
        }

        public string Cache
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string ExtensionsToInclude
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public ICommand LoadCommand => new DelegateCommand(Load);

        public string PathsToExclude
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string ProjectBase
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string Provider
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public ICommand SaveCommand => new DelegateCommand(Save);
        public ICommand SelectCacheCommand => new DelegateCommand(SelectCacheDirectory);
        public ICommand SelectProjectBaseCommand => new DelegateCommand(SelectProjectBaseDirectory);

        public string WorkItemRegEx
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        protected override IEnumerable<string> ValidateProperty(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(Cache):
                    if (!Model.IsCacheValid())
                    {
                        return new[] { "directory_does_not_exist" };
                    }

                    break;

                case nameof(ProjectBase):
                    if (!Directory.Exists(ProjectBase))
                    {
                        return new[] { "directory_does_not_exist" };
                    }

                    break;
            }

            return null;
        }

        private void ApplySettings(Window wnd)
        {
            Apply();
            wnd.Close();
        }

        private void Load()
        {
            var file = _dialogs.GetLoadFile("xml");
            if (file != null)
            {
                Model.LoadFrom(file);
                UpdatAll();
            }
        }

        private void Save()
        {
            Apply();
            var file = _dialogs.GetSaveFile("xml");
            if (file != null)
            {
                Model.SaveTo(file);
            }
        }

        private void SelectCacheDirectory()
        {
            var dir = _dialogs.GetDirectory();
            if (dir != null)
            {
                Cache = dir;
            }
        }

        private void SelectProjectBaseDirectory()
        {
            var dir = _dialogs.GetDirectory();
            if (dir != null)
            {
                ProjectBase = dir;
            }
        }

        private void UpdatAll()
        {
            // Reset to the state of the model element.
            ClearModifications();
            OnAllPropertyChanged();
            ValidateNow(nameof(Cache));
            ValidateNow(nameof(ProjectBase));
        }
    }
}