using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace EdgePilot.Platform;

/// <summary>
/// Makes a transient EdgePilot surface passive to pointer input without relying on
/// WM_NCHITTEST/HTTRANSPARENT. Windows layered windows use WS_EX_TRANSPARENT so mouse
/// events pass to windows underneath; X11/XWayland applies an explicit empty ShapeInput.
/// </summary>
internal sealed class PlatformPassiveSurface : IDisposable
{
    private readonly Window _window;
    private IntPtr _hwnd;
    private IntPtr _xConnection;
    private uint _xid;
    private bool _windowsInitializationAttempted;
    private bool _x11InitializationAttempted;
    private bool _ready;

    public PlatformPassiveSurface(Window window) => _window = window;

    public bool IsReady => !OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() || _ready;

    public bool TryApply(double scaling, Size logicalWindowSize)
    {
        if (OperatingSystem.IsWindows())
            return TryApplyWindows();
        if (OperatingSystem.IsLinux())
            return TryApplyX11();
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

    private bool TryApplyX11()
    {
        if (_ready)
            return true;
        if (!EnsureX11())
            return false;

        try
        {
            // A zero-rectangle SET creates an empty ShapeInput region. Pass a literal native
            // NULL pointer instead of a zero-length managed array; this keeps XCB marshalling
            // deterministic and makes the entire transient Signal surface pointer-passive.
            var cookie = XcbShapeRectanglesChecked(_xConnection, ShapeSet, ShapeInput, Unsorted,
                _xid, 0, 0, 0, IntPtr.Zero);
            var error = XcbRequestCheck(_xConnection, cookie);
            if (error != IntPtr.Zero)
            {
                LibcFree(error);
                System.Diagnostics.Trace.WriteLine("The X server rejected EdgePilot Signal's empty ShapeInput region.");
                return FailX11();
            }

            if (XcbConnectionHasError(_xConnection) != 0)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot Signal lost its X11 passive-input connection.");
                return FailX11();
            }

            _ready = true;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot Signal could not apply passive X11 input: {ex.Message}");
            return FailX11();
        }
    }

    private bool EnsureX11()
    {
        if (_xConnection != IntPtr.Zero && _xid != 0)
            return true;
        if (_x11InitializationAttempted)
            return false;

        _x11InitializationAttempted = true;
        try
        {
            var handle = _window.TryGetPlatformHandle();
            if (handle is null || !string.Equals(handle.HandleDescriptor, "XID", StringComparison.OrdinalIgnoreCase)
                || handle.Handle == IntPtr.Zero)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot Signal passive input requires an X11/XWayland XID.");
                return false;
            }

            _xConnection = XcbConnect(IntPtr.Zero, out _);
            if (_xConnection == IntPtr.Zero || XcbConnectionHasError(_xConnection) != 0)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot Signal could not open an XCB connection for passive input.");
                DisconnectX11();
                return false;
            }

            var versionCookie = XcbShapeQueryVersion(_xConnection);
            var reply = XcbShapeQueryVersionReply(_xConnection, versionCookie, out var versionError);
            try
            {
                if (versionError != IntPtr.Zero || reply == IntPtr.Zero)
                {
                    System.Diagnostics.Trace.WriteLine("EdgePilot Signal could not query the X Shape extension.");
                    return false;
                }

                var version = Marshal.PtrToStructure<XcbShapeVersionReply>(reply);
                if (version.MajorVersion < 1 || (version.MajorVersion == 1 && version.MinorVersion < 1))
                {
                    System.Diagnostics.Trace.WriteLine($"EdgePilot Signal requires X Shape 1.1; server reports {version.MajorVersion}.{version.MinorVersion}.");
                    return false;
                }
            }
            finally
            {
                if (versionError != IntPtr.Zero) LibcFree(versionError);
                if (reply != IntPtr.Zero) LibcFree(reply);
            }

            _xid = unchecked((uint)handle.Handle.ToInt64());
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot Signal could not initialize passive X11 input: {ex.Message}");
            return false;
        }
        finally
        {
            if (_xid == 0)
                DisconnectX11();
        }
    }

    private bool FailX11()
    {
        _ready = false;
        DisconnectX11();
        return false;
    }

    public void Dispose()
    {
        _hwnd = IntPtr.Zero;
        _xid = 0;
        _ready = false;
        DisconnectX11();
    }

    private void DisconnectX11()
    {
        if (_xConnection == IntPtr.Zero)
            return;
        try { XcbDisconnect(_xConnection); }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine(ex);
        }
        _xConnection = IntPtr.Zero;
    }

    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExNoActivate = 0x08000000L;
    private const long WsExLayered = 0x00080000L;
    private const byte ShapeSet = 0;
    private const byte ShapeInput = 2;
    private const byte Unsorted = 0;

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct XcbShapeQueryVersionCookie
    {
        public readonly uint Sequence;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XcbShapeVersionReply
    {
        public byte ResponseType;
        public byte Pad0;
        public ushort Sequence;
        public uint Length;
        public ushort MajorVersion;
        public ushort MinorVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct XcbVoidCookie
    {
        public readonly uint Sequence;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr newLong);

    [DllImport("libxcb.so.1", EntryPoint = "xcb_connect")]
    private static extern IntPtr XcbConnect(IntPtr displayName, out int screen);

    [DllImport("libxcb.so.1", EntryPoint = "xcb_connection_has_error")]
    private static extern int XcbConnectionHasError(IntPtr connection);

    [DllImport("libxcb.so.1", EntryPoint = "xcb_request_check")]
    private static extern IntPtr XcbRequestCheck(IntPtr connection, XcbVoidCookie cookie);

    [DllImport("libxcb.so.1", EntryPoint = "xcb_disconnect")]
    private static extern void XcbDisconnect(IntPtr connection);

    [DllImport("libxcb-shape.so.0", EntryPoint = "xcb_shape_query_version")]
    private static extern XcbShapeQueryVersionCookie XcbShapeQueryVersion(IntPtr connection);

    [DllImport("libxcb-shape.so.0", EntryPoint = "xcb_shape_query_version_reply")]
    private static extern IntPtr XcbShapeQueryVersionReply(IntPtr connection,
        XcbShapeQueryVersionCookie cookie, out IntPtr error);

    [DllImport("libxcb-shape.so.0", EntryPoint = "xcb_shape_rectangles_checked")]
    private static extern XcbVoidCookie XcbShapeRectanglesChecked(IntPtr connection, byte operation,
        byte destinationKind, byte ordering, uint destinationWindow, short xOffset, short yOffset,
        uint rectanglesLength, IntPtr rectangles);

    [DllImport("libc.so.6", EntryPoint = "free")]
    private static extern void LibcFree(IntPtr pointer);
}
