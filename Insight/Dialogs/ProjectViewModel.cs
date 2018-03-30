using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

using Insight.GitProvider;
using Insight.WpfCore;

using Prism.Commands;

namespace Insight.Dialogs
{
    internal sealed class ProjectViewModel : CachedModelWrapper<Project>
    {
        private readonly DialogService _dialogs;

        public ProjectViewModel(Project project, DialogService dialogs) : base(project)
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

                var gitLinear = new ProviderDescription
                {
                    Class = GitProviderLinear.GetClass(),
                    Name = "Git (Caution! Assumes a linear history.)"
                };

                var git = new ProviderDescription
                          {
                                  Class = GitProvider.GitProvider.GetClass(),
                                  Name = "Git (Recovers history file by file)"
                          };

                return new List<ProviderDescription>
                       {
                               svn
                               ,gitLinear,
                               git
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

        public string PathsToInclude
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
                    if (!Directory.Exists(Cache))
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

        public bool Changed { get; private set; }
        private void ApplySettings(Window wnd)
        {
            Changed |= Apply();
            Model.Save();
            wnd.DialogResult = true;
            wnd.Close();
        }

        private void Load()
        {
            var file = _dialogs.GetLoadFile("xml", "Load project", Cache);
            if (file != null)
            {
                Changed = true;
                Model.LoadFrom(file);
                UpdatAll();
            }
        }

        private void Save()
        {
            Apply();
            var file = _dialogs.GetSaveFile("xml", "Save project", Cache);
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