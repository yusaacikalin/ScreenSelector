using Microsoft.Win32;

namespace ScreenSelector;

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ScreenSelector";

    public static void SetEnabled(bool enabled, bool startMinimized)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKey, writable: true);

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var argument = startMinimized ? " --minimized" : string.Empty;
        key.SetValue(ValueName, $"\"{Application.ExecutablePath}\"{argument}");
    }
}
