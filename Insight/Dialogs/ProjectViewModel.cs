using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

using Insight.WpfCore;

using Prism.Commands;

namespace Insight.Dialogs
{
    internal sealed class ProjectViewModel : CachedModelWrapper<Project>
    {
        private readonly DialogService _dialogs;
        private readonly Mode _mode;

        public ProjectViewModel(Project project, DialogService dialogs, Mode mode) : base(project)
        {
            _dialogs = dialogs;
            _mode = mode;
            UpdatAll();
        }

        public enum Mode
        {
            Create,
            Update
        }

        public ICommand OkCommand => new DelegateCommand<Window>(OkClick);

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
                                  Name = "Git (Processes history file by file - slow!)"
                          };

                return new List<ProviderDescription>
                       {
                               svn,
                               git
                       };
            }
        }

        public string ProjectName
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string ProjectParentDirectory
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string SourceControlDirectory
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public string ExtensionsToInclude
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Properties that can only be set when creating the project
        /// </summary>
        public bool EnableImmutableProperties { get; set; }

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


        public string Provider
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public ICommand CancelCommand => new DelegateCommand<Window>(Cancel);


        public ICommand SelectProjectParentCommand => new DelegateCommand(SelectProjectParentDirectory);
        public ICommand SelectSourceControlCommand => new DelegateCommand(SelectSourceControlDirectory);

        public string WorkItemRegEx
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        public bool Changed { get; private set; }

        protected override IEnumerable<string> ValidateProperty(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(ProjectParentDirectory):
                    if (!Directory.Exists(ProjectParentDirectory))
                    {
                        return new[] { "directory_does_not_exist" };
                    }

                    break;

                case nameof(SourceControlDirectory):
                    if (!Directory.Exists(SourceControlDirectory))
                    {
                        return new[] { "directory_does_not_exist" };
                    }

                    break;

                case nameof(ProjectName):
                    if (!IsProjectNameValid())
                    {
                        return new[] { "invalid_characters" };
                    }

                    break;
            }

            return null;
        }

        private void Cancel(Window wnd)
        {
            wnd.DialogResult = false;
            wnd.Close();
        }

        private bool IsProjectNameValid()
        {
            if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            return true;
        }

        private void OkClick(Window wnd)
        {
            Changed |= Apply();

            try
            {
                if (_mode == Mode.Create)
                {
                    if (CreateNewProjectEnvironment() is false)
                    {
                        return;
                    }
                }

                // Create or update
                Model.Save();
                wnd.DialogResult = true;
                wnd.Close();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(Strings.CreateProjectFailed + "\n" + ex.Message);
            }
        }

        private bool CreateNewProjectEnvironment()
        {
            if (Directory.Exists(Model.GetProjectDirectory()))
            {
                var result = _dialogs.AskYesNoQuestion(string.Format(Strings.OverrideExistingProject, Model.GetProjectFile()), Strings.Override);
                if (result is false)
                {
                    // Keep the existing directory
                    return false;
                }
            }

            Model.InitProjectEnvironment();
            return true;
        }


        private void SelectProjectParentDirectory()
        {
            var dir = _dialogs.GetDirectory();
            if (dir != null)
            {
                ProjectParentDirectory = dir;
            }
        }

        private void SelectSourceControlDirectory()
        {
            var dir = _dialogs.GetDirectory();
            if (dir != null)
            {
                SourceControlDirectory = dir;
            }
        }

        private void UpdatAll()
        {
            // Reset to the state of the model element.
            ClearModifications();
            OnAllPropertyChanged();
            ValidateNow(nameof(ProjectName));
            ValidateNow(nameof(ProjectParentDirectory));
            ValidateNow(nameof(SourceControlDirectory));
        }
    }
}