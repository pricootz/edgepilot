using Avalonia;
using Avalonia.Media;

namespace EdgePilot.UI;

internal static class EdgeNotchGeometry
{
    private const double InputStripHeight = 2;

    public static Geometry BuildRight(double windowWidth, double windowHeight, double depth, double length)
    {
        var shape = Calculate(windowWidth, windowHeight, depth, length);
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(shape.Right, shape.Top), true);

        if (shape.Flare > 0.1)
        {
            ctx.ArcTo(
                new Point(shape.Right - shape.Flare, shape.BodyTop),
                new Size(shape.Flare, shape.Flare),
                0,
                false,
                SweepDirection.Clockwise,
                true);
        }

        ctx.LineTo(new Point(shape.Left + shape.Corner, shape.BodyTop), true);

        if (shape.Corner > 0.1)
        {
            ctx.ArcTo(
                new Point(shape.Left, shape.BodyTop + shape.Corner),
                new Size(shape.Corner, shape.Corner),
                0,
                false,
                SweepDirection.CounterClockwise,
                true);
        }

        ctx.LineTo(new Point(shape.Left, shape.BodyBottom - shape.Corner), true);

        if (shape.Corner > 0.1)
        {
            ctx.ArcTo(
                new Point(shape.Left + shape.Corner, shape.BodyBottom),
                new Size(shape.Corner, shape.Corner),
                0,
                false,
                SweepDirection.CounterClockwise,
                true);
        }

        ctx.LineTo(new Point(shape.Right - shape.Flare, shape.BodyBottom), true);

        if (shape.Flare > 0.1)
        {
            ctx.ArcTo(
                new Point(shape.Right, shape.Bottom),
                new Size(shape.Flare, shape.Flare),
                0,
                false,
                SweepDirection.Clockwise,
                true);
        }

        ctx.LineTo(new Point(shape.Right, shape.Top), true);
        ctx.EndFigure(true);
        return geometry;
    }

    /// <summary>
    /// Returns a conservative union of thin rectangles that lies inside the visible notch.
    /// Native window/input regions are rectangular, so this scanline representation prevents
    /// the transparent corners and concave shoulders of the notch's bounding box from becoming
    /// invisible click blockers. The intentional Hover hot-zone is added separately by EdgeWindow.
    /// </summary>
    public static Rect[] BuildInputStripsRight(double windowWidth, double windowHeight,
        double depth, double length)
    {
        var shape = Calculate(windowWidth, windowHeight, depth, length);
        var result = new List<Rect>((int)Math.Ceiling((shape.Bottom - shape.Top) / InputStripHeight));

        for (var y = shape.Top; y < shape.Bottom - 0.001; y += InputStripHeight)
        {
            var bottom = Math.Min(shape.Bottom, y + InputStripHeight);
            // The left contour is monotonic toward the center and then monotonic away from it.
            // Taking the larger endpoint therefore under-approximates the curved silhouette for
            // the whole strip instead of accidentally claiming transparent pixels as input.
            var left = Math.Max(LeftAt(shape, y), LeftAt(shape, bottom));
            if (shape.Right - left > 0.01 && bottom - y > 0.01)
                result.Add(new Rect(left, y, shape.Right - left, bottom - y));
        }

        return result.ToArray();
    }

    private static double LeftAt(NotchShape shape, double y)
    {
        y = Math.Clamp(y, shape.Top, shape.Bottom);

        if (shape.Flare > 0.1 && y < shape.BodyTop)
        {
            var dy = y - shape.Top;
            return shape.Right - shape.Flare +
                   Math.Sqrt(Math.Max(0, shape.Flare * shape.Flare - dy * dy));
        }

        if (shape.Corner > 0.1 && y < shape.BodyTop + shape.Corner)
        {
            var centerY = shape.BodyTop + shape.Corner;
            var dy = y - centerY;
            return shape.Left + shape.Corner -
                   Math.Sqrt(Math.Max(0, shape.Corner * shape.Corner - dy * dy));
        }

        if (shape.Corner > 0.1 && y > shape.BodyBottom - shape.Corner)
        {
            var centerY = shape.BodyBottom - shape.Corner;
            var dy = y - centerY;
            return shape.Left + shape.Corner -
                   Math.Sqrt(Math.Max(0, shape.Corner * shape.Corner - dy * dy));
        }

        if (shape.Flare > 0.1 && y > shape.BodyBottom)
        {
            var dy = y - shape.Bottom;
            return shape.Right - shape.Flare +
                   Math.Sqrt(Math.Max(0, shape.Flare * shape.Flare - dy * dy));
        }

        return shape.Left;
    }

    private static NotchShape Calculate(double windowWidth, double windowHeight, double depth, double length)
    {
        depth = Math.Max(1, depth);
        length = Math.Max(8, Math.Min(length, windowHeight));

        var top = (windowHeight - length) / 2;
        var bottom = top + length;
        var right = windowWidth;
        var left = right - depth;

        // At rest, use the full pill depth as the radius: a soft half-capsule
        // against the screen edge, with no concave shoulders.
        // Introduce the expanded notch's shoulders smoothly as it opens.
        var progress = Math.Clamp((depth - 10) / (88 - 10), 0, 1);
        var morph = progress * progress * (3 - 2 * progress);
        var corner = Math.Min(depth, 10 + 10 * morph);
        var flare = Math.Min(24 * morph,
            Math.Min(length / 4, Math.Max(0, depth - corner)));
        corner = Math.Min(corner, Math.Max(0, (length - flare * 2) / 2));

        return new NotchShape(top, bottom, right, left, corner, flare,
            top + flare, bottom - flare);
    }

    private readonly record struct NotchShape(double Top, double Bottom, double Right, double Left,
        double Corner, double Flare, double BodyTop, double BodyBottom);
}
