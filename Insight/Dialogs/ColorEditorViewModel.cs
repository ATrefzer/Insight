namespace Insight.Dialogs
{
    sealed class ColorEditorViewModel
    {
        private readonly IColorSchemeManager _colorSchemeManager;

        public ColorEditorViewModel(IColorSchemeManager colorSchemeManager)
        {
            _colorSchemeManager = colorSchemeManager;
        }
    }
}
