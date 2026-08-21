using System.Runtime.InteropServices;

namespace ScreenSelector;

internal static class NativeMethods
{
    internal const int WmHotkey = 0x0312;
    internal const int WmNcLeftButtonDown = 0x00A1;
    internal const int HtCaption = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, HotkeyModifiers modifiers, Keys virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    internal static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
}
