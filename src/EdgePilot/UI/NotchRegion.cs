using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace EdgePilot.UI;

// Clips a window's visible area (and hit area) to the notch silhouette plus, optionally,
// the tooltip rectangle. This lets the real Windows acrylic backdrop — which always fills
// a whole window — show only where the pill and popup are, with everything else falling
// through to the desktop. All coordinates are physical pixels, relative to the client area.
internal static class NotchRegion
{
    private const int WindingFill = 2;
    private const int RgnOr = 2;

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreatePolygonRgn(POINT[] points, int count, int fillMode);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);

    [DllImport("gdi32.dll")]
    private static extern int CombineRgn(IntPtr dest, IntPtr src1, IntPtr src2, int mode);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    // pill == null clears the region (window returns to a plain rectangle).
    public static void Apply(Window window, IReadOnlyList<Point>? pill, Rect? tooltip, double cornerPx)
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero) return;

        if (pill is null || pill.Count < 3)
        {
            SetWindowRgn(handle, IntPtr.Zero, true);
            return;
        }

        var poly = new POINT[pill.Count];
        for (var i = 0; i < pill.Count; i++)
            poly[i] = new POINT { X = (int)Math.Round(pill[i].X), Y = (int)Math.Round(pill[i].Y) };

        var region = CreatePolygonRgn(poly, poly.Length, WindingFill);
        if (region == IntPtr.Zero) return;

        if (tooltip is { } r && r.Width > 1 && r.Height > 1)
        {
            var c = (int)Math.Round(cornerPx * 2);
            var tip = CreateRoundRectRgn(
                (int)Math.Round(r.X), (int)Math.Round(r.Y),
                (int)Math.Round(r.Right), (int)Math.Round(r.Bottom), c, c);
            if (tip != IntPtr.Zero)
            {
                CombineRgn(region, region, tip, RgnOr);
                DeleteObject(tip);
            }
        }

        // SetWindowRgn only takes ownership of the region on success; on failure the
        // window keeps its old region and this handle would leak, so delete it ourselves.
        if (SetWindowRgn(handle, region, true) == 0)
            DeleteObject(region);
    }
}
