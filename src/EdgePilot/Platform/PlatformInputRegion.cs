using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace EdgePilot.Platform;

/// <summary>
/// Constrains the native top-level to EdgePilot's live interaction areas.
/// Visual transparency is not an input-routing guarantee: Windows needs a real HWND region,
/// while X11/XWayland needs an explicit ShapeInput region.
/// </summary>
internal sealed class PlatformInputRegion : IDisposable
{
    private readonly Window _window;

    private IntPtr _hwnd;
    private IntPtr _connection;
    private uint _xid;
    private bool _x11InitializationAttempted;
    private bool _windowsRegionVerified;
    private bool _ready;
    private int? _lastNativeRegionHash;

    public PlatformInputRegion(Window window) => _window = window;

    public bool IsReady => !OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() || _ready;

    public bool TryApply(IReadOnlyList<Rect> logicalRects, double scaling, Size logicalWindowSize)
    {
        // Avalonia can recreate a native top-level while the managed Window survives (for
        // example across hide/show and mixed-DPI display transitions). Never trust a cached
        // region until the current HWND has been compared with the one it was applied to.
        if (OperatingSystem.IsWindows() && !EnsureWindows())
            return false;

        var rectangles = ToNativeRectangles(logicalRects, scaling, logicalWindowSize);
        var hash = HashNativeRectangles(rectangles);
        if (_ready && _lastNativeRegionHash == hash)
            return true;

        bool applied;
        if (OperatingSystem.IsWindows())
            applied = TryApplyWindows(rectangles);
        else if (OperatingSystem.IsLinux())
            applied = TryApplyX11(rectangles);
        else
            return true;

        if (applied)
            _lastNativeRegionHash = hash;
        return applied;
    }

    private bool TryApplyWindows(NativeRect[] rectangles)
    {
        if (!EnsureWindows())
            return false;

        IntPtr region = IntPtr.Zero;
        try
        {
            region = CreateWindowsRegion(rectangles);
            if (region == IntPtr.Zero)
                return FailWindows("EdgePilot could not create the Windows input-safe window region.");

            // SetWindowRgn transfers ownership of HRGN to USER32 on success. Unlike
            // HTTRANSPARENT, the HWND simply does not exist for hit testing outside this region,
            // so clicks can reach windows owned by other applications/processes.
            if (SetWindowRgn(_hwnd, region, true) == 0)
                return FailWindows("EdgePilot could not apply the Windows input-safe window region.");

            region = IntPtr.Zero;

            if (!_windowsRegionVerified && !VerifyWindowsRegion(rectangles))
                return FailWindows("EdgePilot could not verify the applied Windows window region.");

            _windowsRegionVerified = true;
            _ready = true;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return FailWindows($"EdgePilot could not apply the Windows input region: {ex.Message}");
        }
        finally
        {
            if (region != IntPtr.Zero)
                DeleteObject(region);
        }
    }

    private bool VerifyWindowsRegion(NativeRect[] rectangles)
    {
        var expected = CreateWindowsRegion(rectangles);
        var actual = CreateRectRgn(0, 0, 0, 0);
        if (expected == IntPtr.Zero || actual == IntPtr.Zero)
        {
            if (expected != IntPtr.Zero) DeleteObject(expected);
            if (actual != IntPtr.Zero) DeleteObject(actual);
            return false;
        }

        try
        {
            if (GetWindowRgn(_hwnd, actual) == RegionError)
                return false;
            return EqualRgn(expected, actual);
        }
        finally
        {
            DeleteObject(expected);
            DeleteObject(actual);
        }
    }

    private static IntPtr CreateWindowsRegion(NativeRect[] rectangles)
    {
        if (rectangles.Length == 0)
            return CreateRectRgn(0, 0, 0, 0);

        var rectSize = Marshal.SizeOf<NativeRect>();
        var headerSize = Marshal.SizeOf<RegionDataHeader>();
        var bufferSize = checked(headerSize + rectSize * rectangles.Length);
        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            var bound = new NativeRect(
                rectangles.Min(rect => rect.Left),
                rectangles.Min(rect => rect.Top),
                rectangles.Max(rect => rect.Right),
                rectangles.Max(rect => rect.Bottom));
            var header = new RegionDataHeader
            {
                Size = (uint)headerSize,
                Type = RectanglesRegionType,
                Count = (uint)rectangles.Length,
                RegionSize = (uint)(rectSize * rectangles.Length),
                Bound = bound
            };
            Marshal.StructureToPtr(header, buffer, false);

            var cursor = IntPtr.Add(buffer, headerSize);
            foreach (var rect in rectangles)
            {
                Marshal.StructureToPtr(rect, cursor, false);
                cursor = IntPtr.Add(cursor, rectSize);
            }

            return ExtCreateRegion(IntPtr.Zero, (uint)bufferSize, buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private bool EnsureWindows()
    {
        var handle = _window.TryGetPlatformHandle();
        if (handle is null || !string.Equals(handle.HandleDescriptor, "HWND", StringComparison.OrdinalIgnoreCase)
            || handle.Handle == IntPtr.Zero)
        {
            System.Diagnostics.Trace.WriteLine("EdgePilot input-region safety requires a valid HWND on Windows.");
            return false;
        }

        if (_hwnd != IntPtr.Zero && _hwnd != handle.Handle)
        {
            // SetWindowRgn belongs to the native HWND, not the managed Avalonia Window. A new
            // handle starts as a full rectangle, so force a fresh application and verification.
            _ready = false;
            _windowsRegionVerified = false;
            _lastNativeRegionHash = null;
        }

        _hwnd = handle.Handle;
        return true;
    }

    private bool FailWindows(string message)
    {
        System.Diagnostics.Trace.WriteLine(message);
        _ready = false;
        _lastNativeRegionHash = null;
        return false;
    }

    private bool TryApplyX11(NativeRect[] nativeRects)
    {
        if (!EnsureX11())
            return false;

        var rectangles = nativeRects.Select(rect => new XRectangle
        {
            X = (short)Math.Clamp(rect.Left, short.MinValue, short.MaxValue),
            Y = (short)Math.Clamp(rect.Top, short.MinValue, short.MaxValue),
            Width = (ushort)Math.Clamp(rect.Right - rect.Left, 0, ushort.MaxValue),
            Height = (ushort)Math.Clamp(rect.Bottom - rect.Top, 0, ushort.MaxValue)
        }).ToArray();

        try
        {
            // XCB uses a connection independent from Avalonia's own X11 connection. A checked
            // SHAPE request lets the package smoke test prove that the server accepted ShapeInput,
            // not merely that the request could be queued locally.
            var cookie = XcbShapeRectanglesChecked(_connection, ShapeSet, ShapeInput, Unsorted,
                _xid, 0, 0, (uint)rectangles.Length, rectangles);
            var error = XcbRequestCheck(_connection, cookie);
            if (error != IntPtr.Zero)
            {
                LibcFree(error);
                System.Diagnostics.Trace.WriteLine("The X server rejected EdgePilot's ShapeInput region.");
                _ready = false;
                _lastNativeRegionHash = null;
                return false;
            }

            if (XcbConnectionHasError(_connection) != 0)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot lost the X11 connection used for input-region safety.");
                _ready = false;
                _lastNativeRegionHash = null;
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot could not apply the X11 input region: {ex.Message}");
            _ready = false;
            _lastNativeRegionHash = null;
            return false;
        }
    }

    private bool EnsureX11()
    {
        if (_ready && _connection != IntPtr.Zero)
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
                System.Diagnostics.Trace.WriteLine("EdgePilot input-region safety requires an X11/XWayland XID; the edge surface will not accept input on this backend.");
                return false;
            }

            _connection = XcbConnect(IntPtr.Zero, out _);
            if (_connection == IntPtr.Zero || XcbConnectionHasError(_connection) != 0)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot could not open an XCB connection for input-region safety.");
                DisconnectX11();
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

                var version = Marshal.PtrToStructure<XcbShapeVersionReply>(reply);
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
                DisconnectX11();
        }
    }

    private static NativeRect[] ToNativeRectangles(IReadOnlyList<Rect> logicalRects, double scaling,
        Size logicalWindowSize)
    {
        scaling = scaling > 0 && double.IsFinite(scaling) ? scaling : 1;
        var windowWidth = Math.Max(0, (int)Math.Ceiling(logicalWindowSize.Width * scaling));
        var windowHeight = Math.Max(0, (int)Math.Ceiling(logicalWindowSize.Height * scaling));
        var result = new List<NativeRect>(logicalRects.Count);

        foreach (var rect in logicalRects)
        {
            if (rect.Width <= 0 || rect.Height <= 0 || !double.IsFinite(rect.X) || !double.IsFinite(rect.Y)
                || !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height))
                continue;

            // Region coordinates are integral device pixels. Nearest-pixel quantization keeps
            // adjacent one-DIP scanlines sharing the exact same boundary at fractional DPI, so
            // SetWindowRgn cannot cut visible one-pixel seams through the animated notch.
            var left = Quantize(rect.Left * scaling, windowWidth);
            var top = Quantize(rect.Top * scaling, windowHeight);
            var right = Quantize(rect.Right * scaling, windowWidth);
            var bottom = Quantize(rect.Bottom * scaling, windowHeight);
            if (right <= left || bottom <= top)
                continue;

            result.Add(new NativeRect(left, top, right, bottom));
        }

        return result.ToArray();
    }

    private static int Quantize(double value, int maximum) =>
        Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, maximum);

    private static int HashNativeRectangles(NativeRect[] rectangles)
    {
        var hash = new HashCode();
        hash.Add(rectangles.Length);
        foreach (var rect in rectangles)
        {
            hash.Add(rect.Left);
            hash.Add(rect.Top);
            hash.Add(rect.Right);
            hash.Add(rect.Bottom);
        }
        return hash.ToHashCode();
    }

    public void Dispose()
    {
        _ready = false;
        _windowsRegionVerified = false;
        _lastNativeRegionHash = null;
        _hwnd = IntPtr.Zero;
        _xid = 0;
        DisconnectX11();
    }

    private void DisconnectX11()
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

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public NativeRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RegionDataHeader
    {
        public uint Size;
        public uint Type;
        public uint Count;
        public uint RegionSize;
        public NativeRect Bound;
    }

    private const uint RectanglesRegionType = 1;
    private const int RegionError = 0;
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

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [DllImport("user32.dll")]
    private static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll")]
    private static extern IntPtr ExtCreateRegion(IntPtr transform, uint dataSize, IntPtr regionData);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EqualRgn(IntPtr first, IntPtr second);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr objectHandle);

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
        uint rectanglesLength, [In] XRectangle[] rectangles);

    [DllImport("libc.so.6", EntryPoint = "free")]
    private static extern void LibcFree(IntPtr pointer);
}
