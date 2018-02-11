using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Insight
{
    /// <summary>
    /// Interaction logic for ProgressViewxaml.xaml
    /// </summary>
    public sealed partial class ProgressView
    {
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint SC_CLOSE = 0xF060;

        private const int WM_SHOWWINDOW = 0x00000018;

        public ProgressView()
        {
            InitializeComponent();
        }

        public bool CanClose { get; internal set; }

        protected override void OnSourceInitialized(EventArgs e)
        {
            // This event is raised to support interoperation with Win32
            base.OnSourceInitialized(e);

            // Disable X button in menu.
            var hWnd = new WindowInteropHelper(this);
            var sysMenu = GetSystemMenu(hWnd.Handle, false);
            EnableMenuItem(sysMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
        }

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !CanClose;
        }
    }
}