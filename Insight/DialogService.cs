using System.Runtime.Versioning;
using System.Windows;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace Insight
{
    public sealed class DialogService
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

        /// <summary>
        /// The extension is just the extension without any . or *
        /// </summary>
        public string GetLoadFile(string extension, string title, string initDirectory)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Multiselect = false;
            dlg.Title = title;

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
        public string GetSaveFile(string extension, string title, string initDirectory)
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

            dlg.Title = title;

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
            {
                return null;
            }

            return dlg.FileName;
        }

        public bool AskYesNoQuestion(string msg, string caption)
        {
            var result = MessageBox.Show(msg, caption, MessageBoxButton.YesNo);
            return result == MessageBoxResult.Yes;
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}