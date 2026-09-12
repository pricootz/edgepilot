using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace EdgePilot.Platform;

/// <summary>
/// Makes the visible EdgePilot HWND a render-only surface on Windows. The visual window remains
/// fully antialiased and never owns pointer input; a separate shaped overlay receives clicks only
/// inside EdgePilot's live regions.
/// </summary>
internal static class WindowsVisualClickThrough
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExLayered = 0x00080000L;
    private const long WsExNoActivate = 0x08000000L;

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;

    public static bool TryEnable(Window window)
    {
        if (!OperatingSystem.IsWindows())
            return true;

        try
        {
            var handle = window.TryGetPlatformHandle();
            if (handle is null ||
                !string.Equals(handle.HandleDescriptor, "HWND", StringComparison.OrdinalIgnoreCase) ||
                handle.Handle == IntPtr.Zero)
                return false;

            var hwnd = handle.Handle;
            var current = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            var required = WsExLayered | WsExTransparent | WsExNoActivate;
            if ((current & required) == required)
                return true;

            var desired = current | required;

            _ = SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(desired));
            _ = SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);

            var verify = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            return (verify & WsExLayered) != 0 &&
                   (verify & WsExTransparent) != 0 &&
                   (verify & WsExNoActivate) != 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot could not enable the Windows render-only surface: {ex.Message}");
            return false;
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint flags);
}
