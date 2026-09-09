using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Path = Avalonia.Controls.Shapes.Path;

namespace EdgePilot.GlassMaskProbe;

internal static class Program
{
    internal static string Mode { get; private set; } = "acrylic-dual";

    [STAThread]
    public static void Main(string[] args)
    {
        Mode = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "acrylic-dual";
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<ProbeApp>()
            .UsePlatformDetect()
            .LogToTrace();
}

internal sealed class ProbeApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new ProbeWindow(Program.Mode);
        base.OnFrameworkInitializationCompleted();
    }
}

internal static class ProbeShape
{
    public const double W = 180;
    public const double H = 620;
    public const double Depth = 92;
    public const double Length = 430;
    public const double Corner = 20;
    public const double Flare = 24;

    public static Geometry Build()
    {
        var top = (H - Length) / 2;
        var bottom = top + Length;
        var right = W;
        var left = right - Depth;
        var bodyTop = top + Flare;
        var bodyBottom = bottom - Flare;

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(right, top), true);
        ctx.ArcTo(new Point(right - Flare, bodyTop), new Size(Flare, Flare),
            0, false, SweepDirection.Clockwise, true);
        ctx.LineTo(new Point(left + Corner, bodyTop), true);
        ctx.ArcTo(new Point(left, bodyTop + Corner), new Size(Corner, Corner),
            0, false, SweepDirection.CounterClockwise, true);
        ctx.LineTo(new Point(left, bodyBottom - Corner), true);
        ctx.ArcTo(new Point(left + Corner, bodyBottom), new Size(Corner, Corner),
            0, false, SweepDirection.CounterClockwise, true);
        ctx.LineTo(new Point(right - Flare, bodyBottom), true);
        ctx.ArcTo(new Point(right, bottom), new Size(Flare, Flare),
            0, false, SweepDirection.Clockwise, true);
        ctx.LineTo(new Point(right, top), true);
        ctx.EndFigure(true);
        return geometry;
    }

    public static double LeftAt(double y)
    {
        var top = (H - Length) / 2;
        var bottom = top + Length;
        var right = W;
        var left = right - Depth;
        var bodyTop = top + Flare;
        var bodyBottom = bottom - Flare;

        y = Math.Clamp(y, top, bottom);

        if (y < bodyTop)
        {
            var dy = y - top;
            return right - Flare + Math.Sqrt(Math.Max(0, Flare * Flare - dy * dy));
        }

        if (y < bodyTop + Corner)
        {
            var centerY = bodyTop + Corner;
            var dy = y - centerY;
            return left + Corner - Math.Sqrt(Math.Max(0, Corner * Corner - dy * dy));
        }

        if (y > bodyBottom - Corner && y <= bodyBottom)
        {
            var centerY = bodyBottom - Corner;
            var dy = y - centerY;
            return left + Corner - Math.Sqrt(Math.Max(0, Corner * Corner - dy * dy));
        }

        if (y > bodyBottom)
        {
            var dy = y - bottom;
            return right - Flare + Math.Sqrt(Math.Max(0, Flare * Flare - dy * dy));
        }

        return left;
    }
}

internal sealed class ProbeWindow : Window
{
    private InputOverlayWindow? _inputOverlay;
    private readonly TextBlock _status;

    public ProbeWindow(string mode)
    {
        Width = ProbeShape.W;
        Height = ProbeShape.H;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;

        var geometry = ProbeShape.Build();
        var content = BuildMetricContent(geometry, mode, out _status);

        switch (mode)
        {
            case "flat-dual":
            case "flat":
                content.Children.Insert(0, new Path
                {
                    Data = geometry,
                    Fill = new SolidColorBrush(Color.Parse("#050608")),
                    IsHitTestVisible = false
                });
                break;

            case "mica-dual":
            case "mica-local":
                content.Children.Insert(0, AcrylicLayer(geometry,
                    tint: Color.Parse("#202124"), tintOpacity: 0.76,
                    materialOpacity: 0.42, fallback: Color.Parse("#303238")));
                break;

            case "acrylic-dual":
            case "acrylic-local":
            default:
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
                area.Right - (int)Math.Round(ProbeShape.W * RenderScaling),
                area.Y + (area.Height - (int)Math.Round(ProbeShape.H * RenderScaling)) / 2);

            if (!OperatingSystem.IsWindows())
            {
                _status.Text = "visual only — dual input probe is Windows-only";
                return;
            }

            // The visible window must never own the mouse. It remains a normal GPU-rendered,
            // antialiased transparent surface; input is delegated to a second invisible HWND.
            var visualHandle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (visualHandle == IntPtr.Zero || !EnableWindow(visualHandle, false))
            {
                // EnableWindow returns zero when the window was previously enabled even if the
                // call succeeds, so verify the resulting state rather than trusting the return.
                if (visualHandle == IntPtr.Zero || IsWindowEnabled(visualHandle))
                {
                    _status.Text = "visual input disable FAILED";
                    return;
                }
            }

            _inputOverlay = new InputOverlayWindow(this, () =>
            {
                _status.Text = $"notch click OK  {DateTime.Now:HH:mm:ss}";
            });
            _inputOverlay.Show();
            _status.Text = "dual-window input active";
        };

        Closed += (_, _) => _inputOverlay?.Close();
    }

    private static ExperimentalAcrylicBorder AcrylicLayer(
        Geometry geometry, Color tint, double tintOpacity,
        double materialOpacity, Color fallback)
    {
        return new ExperimentalAcrylicBorder
        {
            Width = ProbeShape.W,
            Height = ProbeShape.H,
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

    private static Canvas BuildMetricContent(Geometry geometry, string mode, out TextBlock status)
    {
        var canvas = new Canvas
        {
            Width = ProbeShape.W,
            Height = ProbeShape.H,
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

        Canvas.SetLeft(stack, ProbeShape.W - ProbeShape.Depth - 2);
        Canvas.SetTop(stack, (ProbeShape.H - 4 * 84 - 3 * 16) / 2);
        canvas.Children.Add(stack);

        var tag = new TextBlock
        {
            Text = mode,
            FontSize = 8,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(0xC8, 255, 255, 255)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(tag, ProbeShape.W - ProbeShape.Depth + 8);
        Canvas.SetTop(tag, (ProbeShape.H - ProbeShape.Length) / 2 + 12);
        canvas.Children.Add(tag);

        status = new TextBlock
        {
            Text = "starting…",
            FontSize = 7,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#FFB071")),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(status, ProbeShape.W - ProbeShape.Depth + 8);
        Canvas.SetTop(status, (ProbeShape.H + ProbeShape.Length) / 2 - 22);
        canvas.Children.Add(status);

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
            BorderBrush = new SolidColorBrush(Color.Parse("#EEF1F5")),
            Background = new SolidColorBrush(Color.FromArgb(0x18, 0, 0, 0)),
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
                    Foreground = new SolidColorBrush(Color.Parse("#C4CBD5")),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr hWnd, bool enable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr hWnd);
}

internal sealed class InputOverlayWindow : Window
{
    private readonly ProbeWindow _visual;
    private readonly Action _clicked;

    public InputOverlayWindow(ProbeWindow visual, Action clicked)
    {
        _visual = visual;
        _clicked = clicked;

        Width = ProbeShape.W;
        Height = ProbeShape.H;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Opacity = 0.001;
        Content = new Canvas { Background = Brushes.Transparent };

        Opened += (_, _) =>
        {
            Position = _visual.Position;
            ApplyNativeInputRegion();
        };

        PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
                e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                _clicked();
                e.Handled = true;
            }
        };
    }

    private void ApplyNativeInputRegion()
    {
        if (!OperatingSystem.IsWindows()) return;

        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) return;

        var scaling = RenderScaling <= 0 ? 1 : RenderScaling;
        var topPx = (int)Math.Ceiling(((ProbeShape.H - ProbeShape.Length) / 2) * scaling);
        var bottomPx = (int)Math.Floor(((ProbeShape.H + ProbeShape.Length) / 2) * scaling);
        var rightPx = (int)Math.Ceiling(ProbeShape.W * scaling);

        var region = CreateRectRgn(0, 0, 0, 0);
        if (region == IntPtr.Zero) return;

        var success = false;
        try
        {
            for (var yPx = topPx; yPx < bottomPx; yPx++)
            {
                var yDip = (yPx + 0.5) / scaling;
                var leftPx = (int)Math.Ceiling(ProbeShape.LeftAt(yDip) * scaling);
                if (leftPx >= rightPx) continue;

                var strip = CreateRectRgn(leftPx, yPx, rightPx, yPx + 1);
                if (strip == IntPtr.Zero) continue;
                try
                {
                    _ = CombineRgn(region, region, strip, 2); // RGN_OR
                }
                finally
                {
                    _ = DeleteObject(strip);
                }
            }

            success = SetWindowRgn(hwnd, region, true) != 0;
        }
        finally
        {
            // On success Windows owns the HRGN after SetWindowRgn.
            if (!success)
                _ = DeleteObject(region);
        }
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll")]
    private static extern int CombineRgn(IntPtr dest, IntPtr src1, IntPtr src2, int mode);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);
}
