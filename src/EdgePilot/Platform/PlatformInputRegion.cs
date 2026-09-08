using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace EdgePilot.Platform;

/// <summary>
/// Limits the native Linux top-level input shape to EdgePilot's live interaction areas.
/// Avalonia renders transparent pixels, but an X11 top-level remains rectangular for pointer
/// input unless the X Shape input region is explicitly constrained.
/// </summary>
internal sealed class PlatformInputRegion : IDisposable
{
    private readonly Window _window;
    private IntPtr _connection;
    private uint _xid;
    private bool _initializationAttempted;
    private bool _ready;

    public PlatformInputRegion(Window window) => _window = window;

    public bool IsReady => !OperatingSystem.IsLinux() || _ready;

    public bool TryApply(IReadOnlyList<Rect> logicalRects, double scaling, Size logicalWindowSize)
    {
        if (!OperatingSystem.IsLinux())
            return true;

        if (!EnsureX11())
            return false;

        scaling = scaling > 0 && double.IsFinite(scaling) ? scaling : 1;
        var windowWidth = Math.Max(0, (int)Math.Ceiling(logicalWindowSize.Width * scaling));
        var windowHeight = Math.Max(0, (int)Math.Ceiling(logicalWindowSize.Height * scaling));
        var rectangles = ToXRectangles(logicalRects, scaling, windowWidth, windowHeight);

        try
        {
            // XCB uses a connection independent from Avalonia's own X11 connection. This avoids
            // introducing Xlib locking/threading into Avalonia's event loop while still applying
            // SHAPE 1.1's ShapeInput region to the Avalonia-owned XID.
            _ = XcbShapeRectangles(_connection, ShapeSet, ShapeInput, Unsorted, _xid, 0, 0,
                (uint)rectangles.Length, rectangles);
            if (XcbFlush(_connection) <= 0 || XcbConnectionHasError(_connection) != 0)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot lost the X11 connection used for input-region safety.");
                _ready = false;
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot could not apply the X11 input region: {ex.Message}");
            _ready = false;
            return false;
        }
    }

    private bool EnsureX11()
    {
        if (_ready)
            return true;
        if (_initializationAttempted)
            return false;

        _initializationAttempted = true;
        try
        {
            var handle = _window.TryGetPlatformHandle();
            if (handle is null || !string.Equals(handle.HandleDescriptor, "XID", StringComparison.OrdinalIgnoreCase)
                || handle.Handle == IntPtr.Zero)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot input-region safety requires an X11/XWayland XID; the edge surface will not accept input on this backend.");
                return false;
            }

            _connection = XcbConnect(IntPtr.Zero, out _);
            if (_connection == IntPtr.Zero || XcbConnectionHasError(_connection) != 0)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot could not open an XCB connection for input-region safety.");
                Disconnect();
                return false;
            }

            var cookie = XcbShapeQueryVersion(_connection);
            var reply = XcbShapeQueryVersionReply(_connection, cookie, out var error);
            try
            {
                if (error != IntPtr.Zero || reply == IntPtr.Zero)
                {
                    System.Diagnostics.Trace.WriteLine("EdgePilot could not query the X Shape extension.");
                    return false;
                }

                var version = Marshal.PtrToStructure<XcbShapeQueryVersionReply>(reply);
                if (version.MajorVersion < 1 || (version.MajorVersion == 1 && version.MinorVersion < 1))
                {
                    System.Diagnostics.Trace.WriteLine($"EdgePilot requires X Shape 1.1 for ShapeInput; server reports {version.MajorVersion}.{version.MinorVersion}.");
                    return false;
                }
            }
            finally
            {
                if (error != IntPtr.Zero) LibcFree(error);
                if (reply != IntPtr.Zero) LibcFree(reply);
            }

            _xid = unchecked((uint)handle.Handle.ToInt64());
            _ready = true;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot could not initialize X11 input-region safety: {ex.Message}");
            return false;
        }
        finally
        {
            if (!_ready)
                Disconnect();
        }
    }

    private static XRectangle[] ToXRectangles(IReadOnlyList<Rect> logicalRects, double scaling,
        int windowWidth, int windowHeight)
    {
        var result = new List<XRectangle>(logicalRects.Count);
        foreach (var rect in logicalRects)
        {
            if (rect.Width <= 0 || rect.Height <= 0 || !double.IsFinite(rect.X) || !double.IsFinite(rect.Y)
                || !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height))
                continue;

            var left = Math.Clamp((int)Math.Floor(rect.Left * scaling), 0, windowWidth);
            var top = Math.Clamp((int)Math.Floor(rect.Top * scaling), 0, windowHeight);
            var right = Math.Clamp((int)Math.Ceiling(rect.Right * scaling), 0, windowWidth);
            var bottom = Math.Clamp((int)Math.Ceiling(rect.Bottom * scaling), 0, windowHeight);
            if (right <= left || bottom <= top)
                continue;

            result.Add(new XRectangle
            {
                X = (short)Math.Clamp(left, short.MinValue, short.MaxValue),
                Y = (short)Math.Clamp(top, short.MinValue, short.MaxValue),
                Width = (ushort)Math.Clamp(right - left, 0, ushort.MaxValue),
                Height = (ushort)Math.Clamp(bottom - top, 0, ushort.MaxValue)
            });
        }
        return result.ToArray();
    }

    public void Dispose()
    {
        _ready = false;
        _xid = 0;
        Disconnect();
    }

    private void Disconnect()
    {
        if (_connection == IntPtr.Zero)
            return;

        try { XcbDisconnect(_connection); }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine(ex);
        }
        _connection = IntPtr.Zero;
    }

    private const byte ShapeSet = 0;
    private const byte ShapeInput = 2;
    private const byte Unsorted = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct XRectangle
    {
        public short X;
        public short Y;
        public ushort Width;
        public ushort Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct XcbShapeQueryVersionCookie
    {
        public readonly uint Sequence;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XcbShapeQueryVersionReply
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

    [DllImport("libxcb.so.1", EntryPoint = "xcb_connect")]
    private static extern IntPtr XcbConnect(IntPtr displayName, out int screen);

    [DllImport("libxcb.so.1", EntryPoint = "xcb_connection_has_error")]
    private static extern int XcbConnectionHasError(IntPtr connection);

    [DllImport("libxcb.so.1", EntryPoint = "xcb_flush")]
    private static extern int XcbFlush(IntPtr connection);

    [DllImport("libxcb.so.1", EntryPoint = "xcb_disconnect")]
    private static extern void XcbDisconnect(IntPtr connection);

    [DllImport("libxcb-shape.so.0", EntryPoint = "xcb_shape_query_version")]
    private static extern XcbShapeQueryVersionCookie XcbShapeQueryVersion(IntPtr connection);

    [DllImport("libxcb-shape.so.0", EntryPoint = "xcb_shape_query_version_reply")]
    private static extern IntPtr XcbShapeQueryVersionReply(IntPtr connection,
        XcbShapeQueryVersionCookie cookie, out IntPtr error);

    [DllImport("libxcb-shape.so.0", EntryPoint = "xcb_shape_rectangles")]
    private static extern XcbVoidCookie XcbShapeRectangles(IntPtr connection, byte operation,
        byte destinationKind, byte ordering, uint destinationWindow, short xOffset, short yOffset,
        uint rectanglesLength, [In] XRectangle[] rectangles);

    [DllImport("libc.so.6", EntryPoint = "free")]
    private static extern void LibcFree(IntPtr pointer);
}
