using System.Runtime.InteropServices;

namespace ScreenSelector;

internal static class NativeMethods
{
    internal const int WmHotkey = 0x0312;
    internal const int WmNcLeftButtonDown = 0x00A1;
    internal const int HtCaption = 0x0002;
    internal const int RgnDiff = 4;
    internal const int CursorShowing = 0x00000001;
    internal const uint DrawIconNormal = 0x0003;
    internal const uint WdaExcludeFromCapture = 0x00000011;

    [StructLayout(LayoutKind.Sequential)]
    internal struct CursorInfo
    {
        internal int Size;
        internal int Flags;
        internal IntPtr CursorHandle;
        internal Point ScreenPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IconInfo
    {
        [MarshalAs(UnmanagedType.Bool)] internal bool IsIcon;
        internal uint XHotspot;
        internal uint YHotspot;
        internal IntPtr MaskBitmap;
        internal IntPtr ColorBitmap;
    }

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

    [DllImport("user32.dll")]
    internal static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll")]
    internal static extern int CombineRgn(IntPtr destination, IntPtr source1, IntPtr source2, int combineMode);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(IntPtr objectHandle);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmFlush();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorInfo(ref CursorInfo cursorInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetIconInfo(IntPtr icon, out IconInfo iconInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DrawIconEx(IntPtr deviceContext, int x, int y, IntPtr icon,
        int width, int height, uint animationStep, IntPtr flickerFreeBrush, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowDisplayAffinity(IntPtr windowHandle, uint affinity);
}
