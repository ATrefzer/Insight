using Microsoft.WindowsAPICodePack.Dialogs;

namespace Insight
{
    internal sealed class Dialogs
    {
        public string GetDirectory(string initDirectory = null)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.Multiselect = false;
            if (!string.IsNullOrEmpty(initDirectory))
            {
                dlg.InitialDirectory = initDirectory;
            }

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return null;
            }

            return dlg.FileName;
        }

        public string GetLoadFile(string extension, string initDirectory = null)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Multiselect = false;

            if (!string.IsNullOrEmpty(extension))
            {
                dlg.DefaultExtension = extension;
            }

            if (!string.IsNullOrEmpty(initDirectory))
            {
                dlg.InitialDirectory = initDirectory;
            }

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return null;
            }

            return dlg.FileName;
        }

        /// <summary>
        /// I.e. xml, no .*!
        /// </summary>
        public string GetSaveFile(string extension, string initDirectory = null)
        {
            var dlg = new CommonSaveFileDialog();

            if (!string.IsNullOrEmpty(extension))
            {
                dlg.DefaultExtension = extension;
            }

            if (!string.IsNullOrEmpty(initDirectory))
            {
                dlg.InitialDirectory = initDirectory;
            }

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return null;
            }

            return dlg.FileName;
        }
    }
}