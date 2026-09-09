using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace EdgePilot.Platform;

/// <summary>
/// Makes a transient EdgePilot surface passive to pointer input without relying on
/// WM_NCHITTEST/HTTRANSPARENT. Windows layered windows use WS_EX_TRANSPARENT so mouse
/// events pass to windows underneath; X11/XWayland uses an empty ShapeInput region.
/// </summary>
internal sealed class PlatformPassiveSurface : IDisposable
{
    private readonly Window _window;
    private PlatformInputRegion? _linuxInputRegion;
    private IntPtr _hwnd;
    private bool _windowsInitializationAttempted;
    private bool _ready;

    public PlatformPassiveSurface(Window window) => _window = window;

    public bool IsReady => !OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() || _ready;

    public bool TryApply(double scaling, Size logicalWindowSize)
    {
        if (OperatingSystem.IsWindows())
            return TryApplyWindows();

        if (OperatingSystem.IsLinux())
        {
            _linuxInputRegion ??= new PlatformInputRegion(_window);
            _ready = _linuxInputRegion.TryApply(Array.Empty<Rect>(), scaling, logicalWindowSize)
                && _linuxInputRegion.IsReady;
            return _ready;
        }

        return true;
    }

    private bool TryApplyWindows()
    {
        if (_ready)
            return true;
        if (_windowsInitializationAttempted)
            return false;

        _windowsInitializationAttempted = true;
        try
        {
            var handle = _window.TryGetPlatformHandle();
            if (handle is null || !string.Equals(handle.HandleDescriptor, "HWND", StringComparison.OrdinalIgnoreCase)
                || handle.Handle == IntPtr.Zero)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot Signal passive input requires a valid HWND on Windows.");
                return false;
            }

            _hwnd = handle.Handle;
            var current = GetWindowLongPtr(_hwnd, GwlExStyle);
            var desiredBits = current.ToInt64() | WsExLayered | WsExTransparent | WsExNoActivate;
            var desired = new IntPtr(desiredBits);

            Marshal.SetLastPInvokeError(0);
            var previous = SetWindowLongPtr(_hwnd, GwlExStyle, desired);
            var error = Marshal.GetLastPInvokeError();
            if (previous == IntPtr.Zero && error != 0)
            {
                System.Diagnostics.Trace.WriteLine($"EdgePilot Signal could not apply passive Windows styles. Win32 error {error}.");
                return false;
            }

            var applied = GetWindowLongPtr(_hwnd, GwlExStyle).ToInt64();
            var required = WsExLayered | WsExTransparent | WsExNoActivate;
            if ((applied & required) != required)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot Signal could not verify passive Windows styles.");
                return false;
            }

            _ready = true;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot Signal could not initialize passive Windows input: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        _linuxInputRegion?.Dispose();
        _linuxInputRegion = null;
        _hwnd = IntPtr.Zero;
        _ready = false;
    }

    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExNoActivate = 0x08000000L;
    private const long WsExLayered = 0x00080000L;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr newLong);
}
