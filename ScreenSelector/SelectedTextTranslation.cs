using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ScreenSelector;

internal static class SelectedTextTranslation
{
    internal static async Task<string?> ReadSelectionAsync(IntPtr targetWindow, Keys shortcutKey)
    {
        // RegisterHotKey fires on key down. Wait until the user's keys are up so
        // the synthetic Ctrl+C does not inherit an Alt/Shift or another key.
        for (var elapsed = 0; elapsed < 1500; elapsed += 30)
        {
            if (!IsDown(shortcutKey) && !IsDown(Keys.ControlKey) && !IsDown(Keys.ShiftKey) &&
                !IsDown(Keys.Menu) && NativeMethods.GetForegroundWindow() == targetWindow)
                break;
            await Task.Delay(30);
        }

        if (NativeMethods.GetForegroundWindow() != targetWindow || IsDown(shortcutKey) ||
            IsDown(Keys.ControlKey) || IsDown(Keys.ShiftKey) || IsDown(Keys.Menu))
            throw new InvalidOperationException("Kısayol tuşlarını bırakıp seçili metin penceresini açık tutun.");

        var originalClipboard = Clipboard.GetDataObject();
        var clipboardSequence = NativeMethods.GetClipboardSequenceNumber();
        var clipboardChanged = false;
        try
        {
            SendControlKey(Keys.C);
            for (var elapsed = 0; elapsed < 2000; elapsed += 40)
            {
                await Task.Delay(40);
                if (NativeMethods.GetClipboardSequenceNumber() == clipboardSequence) continue;
                clipboardChanged = true;
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            return null;
        }
        finally
        {
            if (clipboardChanged) RestoreClipboard(originalClipboard);
        }
    }

    internal static async Task ReplaceSelectionAsync(IntPtr targetWindow, string translation)
    {
        if (NativeMethods.GetForegroundWindow() != targetWindow)
            throw new InvalidOperationException("Çeviri sırasında etkin pencere değişti; metin değiştirilmedi.");

        var originalClipboard = Clipboard.GetDataObject();
        try
        {
            Clipboard.SetDataObject(translation, true, 5, 100);
            if (NativeMethods.GetForegroundWindow() != targetWindow)
                throw new InvalidOperationException("Etkin pencere değişti; metin değiştirilmedi.");
            SendControlKey(Keys.V);
            // The receiving application may read the clipboard after handling
            // the injected key event, so keep the translation available briefly.
            await Task.Delay(750);
        }
        finally
        {
            RestoreClipboard(originalClipboard);
        }
    }

    private static bool IsDown(Keys key) => (NativeMethods.GetAsyncKeyState((int)key) & 0x8000) != 0;

    private static void RestoreClipboard(IDataObject? data)
    {
        if (data is null) Clipboard.Clear();
        else Clipboard.SetDataObject(data, true, 5, 100);
    }

    private static void SendControlKey(Keys key)
    {
        var inputs = new[]
        {
            Key(Keys.ControlKey, false), Key(key, false), Key(key, true), Key(Keys.ControlKey, true)
        };
        if (NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>()) != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Tuş basımı hedef uygulamaya gönderilemedi.");
    }

    private static NativeMethods.Input Key(Keys key, bool released) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Data = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KeyboardInput
            {
                VirtualKey = (ushort)key,
                Flags = released ? NativeMethods.KeyEventKeyUp : 0
            }
        }
    };
}
