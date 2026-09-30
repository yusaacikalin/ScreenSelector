using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ScreenSelector;

internal sealed class GlobalMouseClickMonitor : IDisposable
{
    private readonly NativeMethods.HookCallback _callback;
    private readonly Action _onClick;
    private IntPtr _hook;

    internal GlobalMouseClickMonitor(Action onClick)
    {
        _onClick = onClick;
        _callback = OnMouseEvent;
        _hook = NativeMethods.SetWindowsHookEx(14 /* WH_MOUSE_LL */, _callback,
            NativeMethods.GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private IntPtr OnMouseEvent(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && message.ToInt64() is 0x0201 or 0x0204 or 0x0207 or 0x020B)
            _onClick(); // The UI queues closure; never block the system hook.
        return NativeMethods.CallNextHookEx(_hook, code, message, data);
    }

    public void Dispose()
    {
        if (_hook == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
        GC.KeepAlive(_callback);
    }
}
