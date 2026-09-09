using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Path = Avalonia.Controls.Shapes.Path;

namespace EdgePilot.GlassMaskProbe;

internal static class Program
{
    internal static string Mode { get; private set; } = "mica-mask";

    [STAThread]
    public static void Main(string[] args)
    {
        Mode = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "mica-mask";
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<ProbeApp>()
            .UsePlatformDetect()
            .LogToTrace();
}

internal sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new ProbeWindow(Program.Mode);
        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class ProbeWindow : Window
{
    private const double W = 180;
    private const double H = 620;
    private const double Depth = 92;
    private const double Length = 430;

    public ProbeWindow(string mode)
    {
        Width = W;
        Height = H;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        TransparencyBackgroundFallback = Brushes.Transparent;
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;

        var geometry = BuildNotch(W, H, Depth, Length);
        var content = BuildMetricContent(geometry, mode);

        switch (mode)
        {
            case "flat":
                TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                content.Children.Insert(0, new Path
                {
                    Data = geometry,
                    Fill = new SolidColorBrush(Color.Parse("#050608")),
                    IsHitTestVisible = false
                });
                break;

            case "mica-mask":
                // Probe A: does Avalonia's antialiased Window.Clip also mask the native Mica backdrop?
                TransparencyLevelHint =
                    [WindowTransparencyLevel.Mica, WindowTransparencyLevel.Transparent];
                Clip = geometry;
                content.Children.Insert(0, new Path
                {
                    Data = geometry,
                    Fill = new SolidColorBrush(Color.FromArgb(0x18, 255, 255, 255)),
                    IsHitTestVisible = false
                });
                break;

            case "acrylic-mask":
                // Probe B: same question for the native Acrylic backdrop.
                TransparencyLevelHint =
                    [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent];
                Clip = geometry;
                content.Children.Insert(0, new Path
                {
                    Data = geometry,
                    Fill = new SolidColorBrush(Color.FromArgb(0x24, 5, 6, 8)),
                    IsHitTestVisible = false
                });
                break;

            case "mica-local":
                // Probe C: GPU-rendered local material clipped by the notch geometry. No native
                // window-region clipping; the window itself stays truly transparent.
                TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                content.Children.Insert(0, AcrylicLayer(geometry,
                    tint: Color.Parse("#202124"), tintOpacity: 0.76,
                    materialOpacity: 0.42, fallback: Color.Parse("#303238")));
                break;

            case "acrylic-local":
            default:
                // Probe D: local Acrylic material clipped by the antialiased geometry.
                TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
                content.Children.Insert(0, AcrylicLayer(geometry,
                    tint: Color.Parse("#090B0F"), tintOpacity: 0.44,
                    materialOpacity: 0.28, fallback: Color.Parse("#24272D")));
                break;
        }

        Content = content;

        Opened += (_, _) =>
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen is null) return;
            var area = screen.WorkingArea;
            Position = new PixelPoint(
                area.Right - (int)Math.Round(W * RenderScaling),
                area.Y + (area.Height - (int)Math.Round(H * RenderScaling)) / 2);
        };

        // Escape closes the disposable probe.
        KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Escape)
                Close();
        };
    }

    private static ExperimentalAcrylicBorder AcrylicLayer(
        Geometry geometry, Color tint, double tintOpacity,
        double materialOpacity, Color fallback)
    {
        return new ExperimentalAcrylicBorder
        {
            Width = W,
            Height = H,
            Clip = geometry,
            IsHitTestVisible = false,
            Material = new ExperimentalAcrylicMaterial
            {
                BackgroundSource = AcrylicBackgroundSource.Digger,
                TintColor = tint,
                TintOpacity = tintOpacity,
                MaterialOpacity = materialOpacity,
                FallbackColor = fallback
            }
        };
    }

    private static Canvas BuildMetricContent(Geometry geometry, string mode)
    {
        var canvas = new Canvas
        {
            Width = W,
            Height = H,
            Background = Brushes.Transparent,
            Clip = geometry
        };

        var stack = new StackPanel
        {
            Width = 92,
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        stack.Children.Add(Ring("C", "4%", "CPU"));
        stack.Children.Add(Ring("M", "47%", "RAM"));
        stack.Children.Add(Ring("D", "81%", "DISCO"));
        stack.Children.Add(Ring("↕", "SÌ", "RETE"));

        Canvas.SetLeft(stack, W - Depth - 2);
        Canvas.SetTop(stack, (H - 4 * 84 - 3 * 16) / 2);
        canvas.Children.Add(stack);

        var tag = new TextBlock
        {
            Text = mode,
            FontSize = 8,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(0xB8, 255, 255, 255)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(tag, W - Depth + 8);
        Canvas.SetTop(tag, (H - Length) / 2 + 12);
        canvas.Children.Add(tag);

        return canvas;
    }

    private static Control Ring(string glyph, string value, string caption)
    {
        var ring = new Border
        {
            Width = 46,
            Height = 46,
            CornerRadius = new CornerRadius(23),
            BorderThickness = new Thickness(3),
            BorderBrush = new SolidColorBrush(Color.Parse("#D7DCE4")),
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = glyph,
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        return new StackPanel
        {
            Width = 92,
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                ring,
                new TextBlock
                {
                    Text = value,
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = caption,
                    FontSize = 8,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#AEB7C4")),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };
    }

    private static Geometry BuildNotch(double windowWidth, double windowHeight, double depth, double length)
    {
        var top = (windowHeight - length) / 2;
        var bottom = top + length;
        var right = windowWidth;
        var left = right - depth;
        const double corner = 20;
        const double flare = 24;
        var bodyTop = top + flare;
        var bodyBottom = bottom - flare;

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(right, top), true);
        ctx.ArcTo(new Point(right - flare, bodyTop), new Size(flare, flare),
            0, false, SweepDirection.Clockwise, true);
        ctx.LineTo(new Point(left + corner, bodyTop), true);
        ctx.ArcTo(new Point(left, bodyTop + corner), new Size(corner, corner),
            0, false, SweepDirection.CounterClockwise, true);
        ctx.LineTo(new Point(left, bodyBottom - corner), true);
        ctx.ArcTo(new Point(left + corner, bodyBottom), new Size(corner, corner),
            0, false, SweepDirection.CounterClockwise, true);
        ctx.LineTo(new Point(right - flare, bodyBottom), true);
        ctx.ArcTo(new Point(right, bottom), new Size(flare, flare),
            0, false, SweepDirection.Clockwise, true);
        ctx.LineTo(new Point(right, top), true);
        ctx.EndFigure(true);
        return geometry;
    }
}
