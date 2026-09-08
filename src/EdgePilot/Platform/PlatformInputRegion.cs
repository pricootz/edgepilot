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
    private IntPtr _display;
    private IntPtr _xid;
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

        // XShapeCombineRectangles with one empty rectangle produces an empty ShapeInput
        // region. Passing a stable array also avoids platform marshalling edge cases for
        // zero-length arrays.
        if (rectangles.Length == 0)
            rectangles = [new XRectangle()];

        try
        {
            XShapeCombineRectangles(_display, _xid, ShapeInput, 0, 0,
                rectangles, rectangles.Length, ShapeSet, Unsorted);
            XFlush(_display);
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

            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot could not open the X11 display for input-region safety.");
                return false;
            }

            if (XShapeQueryExtension(_display, out _, out _) == 0)
            {
                System.Diagnostics.Trace.WriteLine("EdgePilot requires the X Shape extension to constrain the Linux edge input region.");
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
                return false;
            }

            _xid = handle.Handle;
            _ready = true;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine($"EdgePilot could not initialize X11 input-region safety: {ex.Message}");
            if (_display != IntPtr.Zero)
            {
                try { XCloseDisplay(_display); } catch { }
                _display = IntPtr.Zero;
            }
            return false;
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
        _xid = IntPtr.Zero;
        if (_display == IntPtr.Zero)
            return;

        try { XCloseDisplay(_display); }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine(ex);
        }
        _display = IntPtr.Zero;
    }

    private const int ShapeInput = 2;
    private const int ShapeSet = 0;
    private const int Unsorted = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct XRectangle
    {
        public short X;
        public short Y;
        public ushort Width;
        public ushort Height;
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libXext.so.6")]
    private static extern int XShapeQueryExtension(IntPtr display, out int eventBase, out int errorBase);

    [DllImport("libXext.so.6")]
    private static extern void XShapeCombineRectangles(IntPtr display, IntPtr destination, int destinationKind,
        int xOffset, int yOffset, [In] XRectangle[] rectangles, int rectangleCount, int operation, int ordering);
}
