using System;
using System.ComponentModel;
using System.Windows.Interop;

namespace Insight
{
    /// <summary>
    /// Interaction logic for ProgressViewxaml.xaml
    /// </summary>
    public sealed partial class ProgressView
    {   
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
            var sysMenu = NativeMethods.GetSystemMenu(hWnd.Handle, false);
            NativeMethods.EnableMenuItem(sysMenu, NativeMethods.SC_CLOSE, NativeMethods.MF_BYCOMMAND | NativeMethods.MF_GRAYED);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !CanClose;
        }
    }
}