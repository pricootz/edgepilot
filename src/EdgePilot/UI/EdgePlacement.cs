using Avalonia;
using Avalonia.Platform;

namespace EdgePilot.UI;

public enum EdgeSide
{
    Right,
    Left,
    Top,
    Bottom
}

public static class EdgePlacement
{
    public static PixelPoint Calculate(Screen screen, EdgeSide edge, Size sizeInDips)
    {
        var area = screen.WorkingArea;
        var width = Math.Max(1, (int)Math.Ceiling(sizeInDips.Width * screen.Scaling));
        var height = Math.Max(1, (int)Math.Ceiling(sizeInDips.Height * screen.Scaling));

        return edge switch
        {
            EdgeSide.Right => new PixelPoint(
                area.X + area.Width - width,
                area.Y + (area.Height - height) / 2),
            EdgeSide.Left => new PixelPoint(
                area.X,
                area.Y + (area.Height - height) / 2),
            EdgeSide.Top => new PixelPoint(
                area.X + (area.Width - width) / 2,
                area.Y),
            EdgeSide.Bottom => new PixelPoint(
                area.X + (area.Width - width) / 2,
                area.Y + area.Height - height),
            _ => new PixelPoint(area.X + area.Width - width, area.Y + (area.Height - height) / 2)
        };
    }

    public static EdgeSide FromEnvironment()
    {
        var value = Environment.GetEnvironmentVariable("EDGEPILOT_EDGE")?.Trim().ToLowerInvariant();
        return value switch
        {
            "left" => EdgeSide.Left,
            "top" => EdgeSide.Top,
            "bottom" => EdgeSide.Bottom,
            _ => EdgeSide.Right
        };
    }
}
