using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace EdgePilot.UI;

internal sealed class MetricRing : StackPanel
{
    private const double Diameter = 46;
    private const double TrackInset = 4;

    private readonly Path _progress;
    private readonly TextBlock _glyph;
    private readonly TextBlock _value;
    private readonly TextBlock _caption;

    public MetricRing(string glyph, string caption)
    {
        Width = 62;
        Height = 78;
        Spacing = 4;
        HorizontalAlignment = HorizontalAlignment.Center;

        _glyph = new TextBlock
        {
            Text = glyph,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#F4F6F8"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _progress = new Path
        {
            Width = Diameter,
            Height = Diameter,
            Stroke = Brush("#F1F4F7"),
            StrokeThickness = 3,
            StrokeLineCap = PenLineCap.Round,
            Stretch = Stretch.None
        };

        var track = new Ellipse
        {
            Width = Diameter - TrackInset * 2 + 3,
            Height = Diameter - TrackInset * 2 + 3,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Stroke = Brush("#323741"),
            StrokeThickness = 3
        };

        var ring = new Grid
        {
            Width = Diameter,
            Height = Diameter,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ring.Children.Add(track);
        ring.Children.Add(_progress);
        ring.Children.Add(_glyph);

        _value = new TextBlock
        {
            Text = "—",
            FontSize = 11,
            Height = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#F4F6F8"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        _caption = new TextBlock
        {
            Text = caption,
            FontSize = 8,
            Height = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#777F8C"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        Children.Add(ring);
        Children.Add(_value);
        Children.Add(_caption);

        SetValue(null, "—");
    }

    public void SetCaption(string caption) => _caption.Text = caption;

    public void SetValue(double? percent, string text)
    {
        _value.Text = text;
        _progress.Data = percent is null ? null : Arc(Math.Clamp(percent.Value, 0, 100));
        _progress.Opacity = percent is null ? 0 : 1;
    }

    public void SetGlyph(string glyph) => _glyph.Text = glyph;

    private static Geometry Arc(double percent)
    {
        var rect = new Rect(TrackInset, TrackInset, Diameter - TrackInset * 2, Diameter - TrackInset * 2);
        if (percent >= 99.95)
            return new EllipseGeometry(rect);

        if (percent <= 0.05)
            return new StreamGeometry();

        var center = rect.Center;
        var radius = rect.Width / 2;
        var startAngle = -Math.PI / 2;
        var endAngle = startAngle + 2 * Math.PI * (percent / 100.0);
        var start = new Point(center.X + radius * Math.Cos(startAngle), center.Y + radius * Math.Sin(startAngle));
        var end = new Point(center.X + radius * Math.Cos(endAngle), center.Y + radius * Math.Sin(endAngle));

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(start, false);
        ctx.ArcTo(
            end,
            new Size(radius, radius),
            0,
            percent > 50,
            SweepDirection.Clockwise,
            true);
        ctx.EndFigure(false);
        return geometry;
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
}
