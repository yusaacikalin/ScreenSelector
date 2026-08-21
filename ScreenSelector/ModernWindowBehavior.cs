namespace ScreenSelector;

internal static class ModernWindowBehavior
{
    internal static void EnableDragging(Form form, Control topArea)
    {
        RegisterDragSurface(topArea);

        void RegisterDragSurface(Control control)
        {
            control.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left || form.WindowState == FormWindowState.Maximized) return;

                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(form.Handle, NativeMethods.WmNcLeftButtonDown,
                    new IntPtr(NativeMethods.HtCaption), IntPtr.Zero);
            };

            foreach (Control child in control.Controls)
            {
                if (child is Label) RegisterDragSurface(child);
            }
        }
    }
}
