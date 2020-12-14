using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Insight.Shared;
using Insight.ViewModels;
using Insight.WpfCore;

using Prism.Commands;

using Visualization.Controls;
using Visualization.Controls.Interfaces;

namespace Insight.Dialogs
{
    static class ColorPaletteExtensions
    {
        public static IColorScheme ForAlias(this IColorScheme palette, IAliasMapping aliasMapping)
        {
            return new AliasColorScheme(palette, aliasMapping);
        }
    }


    sealed class ColorEditorViewModel : ViewModelBase, ISearchableViewModel
    {
        private readonly IColorSchemeManager _colorSchemeManager;
        private readonly IColorScheme _sourcePalette;
        private readonly IColorPalette _aliasPalette;
        private List<ColorMapping> _allMappings;

        /// <summary>
        /// AllMappings is initialized only once. We only change the colors.
        /// </summary>
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
        public ICommand ColorAssignmentCommand { get; set; }

        public ICommand AddCustomColorCommand { get; set; }


        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public ColorEditorViewModel(IColorSchemeManager colorSchemeManager, IAliasMapping aliasMapping)
        {
            _colorSchemeManager = colorSchemeManager;

            _sourcePalette = _colorSchemeManager.LoadColorScheme();
            _aliasPalette = _sourcePalette.ForAlias(aliasMapping);

            MergeColorsCommand = new DelegateCommand<IReadOnlyList<object>>(OnMergeColorsClick);
            ReleaseCommand = new DelegateCommand<IReadOnlyList<object>>(OnReleaseColorClick);
            ApplyCommand = new DelegateCommand<Window>(OnApplyClick);
            ResetCommand = new DelegateCommand(OnResetClick);
            CloseCommand = new DelegateCommand<Window>(OnCloseClick);
            ColorAssignmentCommand = new DelegateCommand<IReadOnlyList<object>>(OnColorAssignmentClick);
            AddCustomColorCommand = new DelegateCommand<Color?>(AddCustomColorClick);

            ShowOnlyFreeColors = false;

            Init();
        }

        private readonly List<Color> _newCustomColors = new List<Color>();

        private void AddCustomColorClick(Color? newColor)
        {
            if (!newColor.HasValue)
            {
                return;
            }

            if (_aliasPalette.GetAllColors().Contains(newColor.Value) || _newCustomColors.Contains(newColor.Value))
            {
                // The color already exists
                AssignmentColor = newColor.Value;
            }
            else
            {
                _newCustomColors.Add(newColor.Value);

                // Ensure new color is shown.
                UpdateAssignableColors();
            }
        }

        public Color AssignmentColor
        {
            get { return _assignmentColor; }
            set
            {
                _assignmentColor = value;
                OnPropertyChanged();
            }
        }

        private Color _assignmentColor = DefaultDrawingPrimitives.DefaultColor;


        private void OnColorAssignmentClick(IReadOnlyList<object> untypedMappings)
        {
            var mappings = untypedMappings.OfType<ColorMapping>().ToList();
            foreach (var mapping in mappings)
            {
                mapping.Color = AssignmentColor;
            }

            if (ShowOnlyFreeColors == true)
            {
                UpdateAssignableColors();
            }
        }

        void UpdateAssignableColors()
        {
            var allColors = _aliasPalette.GetAllColors().Union(_newCustomColors);
            if (ShowOnlyFreeColors == true)
            {
                // Note: AllMappings is initialized only once
                var usedColors = AllMappings.Select(mapping => mapping.Color).Distinct();
                VisibleColors = allColors.Except(usedColors).ToList();
            }
            else
            {
                VisibleColors = allColors.ToList();
            }
        }

        private void Init()
        {
            // Load initial mappings and colors.
            AllMappings = _aliasPalette.GetColorMappings().ToList();
            UpdateAssignableColors();
            SearchText = "";
        }

        /// <summary>
        /// Makes an automatic new assignment to default colors.
        /// The new color assignments are not stored yet. Only in the color editor.
        /// </summary>
        private void Reset()
        {
            _newCustomColors.Clear();

            // Get existing alias names
            var names = _aliasPalette.GetColorMappings().Select(mapping => mapping.Name).ToArray();

            var tmpPalette = new ColorScheme(names);
            AllMappings  = tmpPalette.GetColorMappings().ToList();

            UpdateAssignableColors();
            SearchText = "";
        }

        public List<Color> VisibleColors
        {
            get => _visibleColors;
            set
            {
                _visibleColors = value;
                OnPropertyChanged();
            }
        }

        private List<Color> _visibleColors;

        public bool? ShowOnlyFreeColors
        {
            get => _showOnlyFreeColors;
            set
            {
                _showOnlyFreeColors = value;
                UpdateAssignableColors();
                OnPropertyChanged();
            }
        }

        private bool? _showOnlyFreeColors;


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
            // AllMappings contain the alias names
            // Here we have to map back the color to the developers.
            // We assign the same color to each developer that shares the same alias.

            
            foreach (var color in _newCustomColors)
            {
                // Ensure all new colors exist even if not used in the mappings.
                _sourcePalette.AddColor(color);
            }

            // Write through to the source palette.
            _aliasPalette.Update(AllMappings);

            _colorSchemeManager.Save(_sourcePalette);
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