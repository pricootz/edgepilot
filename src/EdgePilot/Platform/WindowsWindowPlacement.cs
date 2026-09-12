using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace EdgePilot.Platform;

/// <summary>
/// Applies and verifies an explicit Windows recovery move. Avalonia remains the owner of normal
/// placement; this native path is used only when the user asks EdgePilot to escape an inaccessible
/// display and the result must not depend on a deferred compositor/DPI transition.
/// </summary>
internal static class WindowsWindowPlacement
{
    private const uint MonitorDefaultToNull = 0;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    public static bool TryMoveAndVerify(Window window, PixelPoint position, PixelRect targetBounds)
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

            var targetCenter = new NativePoint(
                targetBounds.X + targetBounds.Width / 2,
                targetBounds.Y + targetBounds.Height / 2);
            var expectedMonitor = MonitorFromPoint(targetCenter, MonitorDefaultToNull);
            if (expectedMonitor == IntPtr.Zero ||
                !SetWindowPos(handle.Handle, IntPtr.Zero, position.X, position.Y, 0, 0,
                    SwpNoSize | SwpNoZOrder | SwpNoActivate))
                return false;

            return MonitorFromWindow(handle.Handle, MonitorDefaultToNull) == expectedMonitor;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or
                                   BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot could not verify its recovery move: {ex.Message}");
            return false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint
    {
        public NativePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly int X;
        public readonly int Y;
    }
}
