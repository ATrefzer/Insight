using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Insight.ViewModels;
using Insight.WpfCore;

using Prism.Commands;

using Visualization.Controls;
using Visualization.Controls.Interfaces;

namespace Insight.Dialogs
{
    sealed class ColorEditorViewModel : ViewModelBase, ISearchableViewModel
    {
        private readonly IColorSchemeManager _colorSchemeManager;
        private readonly IColorScheme _colorScheme;
        private List<ColorMapping> _allMappings;

        public List<ColorMapping> AllMappings
        {
            get => _allMappings;
            set
            {
                _allMappings = value;
                OnPropertyChanged();
            }
        }

        public ICommand MergeColorsCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand ApplyCommand { get; set; }

        public ICommand CloseCommand { get; set; }

        public ICommand ReleaseCommand { get; set; }

        // TODO give name of a color scheme for example teams, anonymous etc.
        public ColorEditorViewModel(IColorSchemeManager colorSchemeManager)
        {
            _colorSchemeManager = colorSchemeManager;
            _colorScheme = _colorSchemeManager.LoadColorScheme();

            MergeColorsCommand = new DelegateCommand<IReadOnlyList<object>>(OnMergeColorsClick);
            ReleaseCommand = new DelegateCommand<IReadOnlyList<object>>(OnReleaseColorClick);
            ApplyCommand = new DelegateCommand<Window>(OnApplyClick);
            ResetCommand = new DelegateCommand(OnResetClick);
            CloseCommand = new DelegateCommand<Window>(OnCloseClick);

            Reset();
        }

        private void Reset()
        {
            // Reload mappings and discards any changes
            AllMappings = _colorScheme.ToList();

            // If there is no color scheme create a new one!

            var allColors = _allMappings.Select(pair => pair.Color).ToList();
        }

        private void OnMergeColorsClick(IReadOnlyList<object> selectedItems)
        {
            var mappings = selectedItems.OfType<ColorMapping>().ToList();
            var source = mappings[0];
            foreach (var mapping in mappings)
            {
                mapping.Color = source.Color;
            }
        }

        private void OnReleaseColorClick(IReadOnlyList<object> selectedItems)
        {
            var color = Color.FromArgb(DefaultDrawingPrimitives.DefaultColor.A,
                                       DefaultDrawingPrimitives.DefaultColor.R,
                                       DefaultDrawingPrimitives.DefaultColor.G,
                                       DefaultDrawingPrimitives.DefaultColor.B
                                      );

            var mappings = selectedItems.OfType<ColorMapping>().ToList();
            foreach (var mapping in mappings)
            {
                mapping.Color = color;
            }
        }

        public void OnCloseClick(Window wnd)
        {
            wnd.Close();
        }


        public void OnApplyClick(Window wnd)
        {
            _colorSchemeManager.UpdateAndSave(_colorScheme, AllMappings);
            wnd.Close();
        }

        public void OnResetClick()
        {
            Reset();
        }


        /// <summary>
        /// Called from code behind to create a filter for the view model data.
        /// </summary>
        public Predicate<object> CreateFilter(string text)
        {
            return obj =>
                   {
                       if (obj is ColorMapping mapping)
                       {
                           if (string.IsNullOrWhiteSpace(text))
                           {
                               return true;
                           }

                           return mapping.Name.ToLowerInvariant().Contains(text.ToLowerInvariant());
                       }

                       return false;
                   };
        }
    }
}