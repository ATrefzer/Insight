using System;
using System.Runtime.InteropServices;

namespace Insight
{
    internal static class NativeMethods
    {
        public const uint MF_BYCOMMAND = 0x00000000;
        public const uint MF_GRAYED = 0x00000001;
        public const uint SC_CLOSE = 0xF060;

        public const int WM_SHOWWINDOW = 0x00000018;

        [DllImport("user32.dll")]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
    }
}
