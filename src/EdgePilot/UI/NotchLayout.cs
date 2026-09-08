using Avalonia;

namespace EdgePilot.UI;

/// <summary>Maps the right-edge design coordinates to each physical edge.</summary>
public static class NotchLayout
{
    public const double DesignWidth = 410;
    public const double DesignHeight = 620;

    public static bool Horizontal(EdgeSide edge) => edge is EdgeSide.Top or EdgeSide.Bottom;
    public static Size WindowSize(EdgeSide edge) => Horizontal(edge)
        ? new Size(DesignHeight, DesignWidth) : new Size(DesignWidth, DesignHeight);

    public static Matrix Transform(EdgeSide edge) => edge switch
    {
        EdgeSide.Left => new Matrix(-1, 0, 0, 1, DesignWidth, 0),
        EdgeSide.Top => new Matrix(0, -1, 1, 0, 0, DesignWidth),
        EdgeSide.Bottom => new Matrix(0, 1, 1, 0, 0, 0),
        _ => Matrix.Identity
    };

    public static Point ToScreen(Point point, EdgeSide edge) => point.Transform(Transform(edge));

    public static Point ToDesign(Point point, EdgeSide edge) => edge switch
    {
        EdgeSide.Left => new Point(DesignWidth - point.X, point.Y),
        EdgeSide.Top => new Point(DesignWidth - point.Y, point.X),
        EdgeSide.Bottom => new Point(point.Y, point.X),
        _ => point
    };

    public static Rect ToScreen(Rect rect, EdgeSide edge)
    {
        var a = ToScreen(rect.TopLeft, edge);
        var b = ToScreen(rect.BottomRight, edge);
        return new Rect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
}
