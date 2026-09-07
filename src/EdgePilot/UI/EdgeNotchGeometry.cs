using Avalonia;
using Avalonia.Media;

namespace EdgePilot.UI;

internal static class EdgeNotchGeometry
{
    public static Geometry BuildRight(double windowWidth, double windowHeight, double depth, double length)
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

        var bodyTop = top + flare;
        var bodyBottom = bottom - flare;

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(right, top), true);

        if (flare > 0.1)
        {
            ctx.ArcTo(
                new Point(right - flare, bodyTop),
                new Size(flare, flare),
                0,
                false,
                SweepDirection.Clockwise,
                true);
        }

        ctx.LineTo(new Point(left + corner, bodyTop), true);

        if (corner > 0.1)
        {
            ctx.ArcTo(
                new Point(left, bodyTop + corner),
                new Size(corner, corner),
                0,
                false,
                SweepDirection.CounterClockwise,
                true);
        }

        ctx.LineTo(new Point(left, bodyBottom - corner), true);

        if (corner > 0.1)
        {
            ctx.ArcTo(
                new Point(left + corner, bodyBottom),
                new Size(corner, corner),
                0,
                false,
                SweepDirection.CounterClockwise,
                true);
        }

        ctx.LineTo(new Point(right - flare, bodyBottom), true);

        if (flare > 0.1)
        {
            ctx.ArcTo(
                new Point(right, bottom),
                new Size(flare, flare),
                0,
                false,
                SweepDirection.Clockwise,
                true);
        }

        ctx.LineTo(new Point(right, top), true);
        ctx.EndFigure(true);
        return geometry;
    }
}
