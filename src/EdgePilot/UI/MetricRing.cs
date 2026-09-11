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
    private readonly Ellipse _track;
    private readonly TextBlock _glyph;
    private readonly TextBlock _value;
    private readonly TextBlock _label;

    public MetricRing(string glyph, string caption)
    {
        Width = 62;
        Height = 76;
        Spacing = 3;
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

        _track = new Ellipse
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
        ring.Children.Add(_track);
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

        _label = new TextBlock
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
        Children.Add(_label);

        SetValue(null, "—");
    }

    public void SetValue(double? percent, string text)
    {
        _value.Text = text;
        _progress.Data = percent is null ? null : Arc(Math.Clamp(percent.Value, 0, 100));
        _progress.Opacity = percent is null ? 0 : 1;
    }

    public void SetGlyph(string glyph) => _glyph.Text = glyph;

    public void SetTheme(bool dark, bool translucent = false)
    {
        if (dark)
        {
            _glyph.Foreground = Brush("#F7F8FA");
            _value.Foreground = Brush("#F7F8FA");
            _label.Foreground = Brush(translucent ? "#D4DAE3" : "#777F8C");
            _progress.Stroke = Brush("#F2F4F7");
            _track.Stroke = Brush(translucent ? "#444B56" : "#323741");
        }
        else
        {
            _glyph.Foreground = Brush("#15171B");
            _value.Foreground = Brush("#15171B");
            _label.Foreground = Brush(translucent ? "#3F434A" : "#55585E");
            _progress.Stroke = Brush("#2A2D32");
            _track.Stroke = Brush(translucent ? "#B9BEC7" : "#C9CDD4");
        }
    }

    public void SetCaption(string caption) => _label.Text = caption;

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
