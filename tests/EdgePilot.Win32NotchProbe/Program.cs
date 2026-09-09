using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Path = Avalonia.Controls.Shapes.Path;

namespace EdgePilot.Win32NotchProbe;

internal static class Program
{
    internal static string Mode { get; private set; } = "flat";

    [STAThread]
    public static void Main(string[] args)
    {
        Mode = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "flat";
        if (Mode is not ("flat" or "mica" or "acrylic")) Mode = "flat";
        AppBuilder.Configure<ProbeApp>().UsePlatformDetect().LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}

internal sealed class ProbeApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new VisualNotchWindow(Program.Mode);
        base.OnFrameworkInitializationCompleted();
    }
}

internal static class NotchShape
{
    public const double W = 180, H = 620, Depth = 92, Length = 430, Corner = 20, Flare = 24;

    // Content lives inside a deliberate safe area instead of being positioned from the HWND.
    // The visible vertical body is 382 DIP high; 4 x 76 DIP metric cells + 3 x 10 DIP gaps
    // consume 334 DIP, leaving an exact 24 DIP optical margin above and below.
    public const double ContentSidePadding = 12;
    public const double MetricCellHeight = 76;
    public const double MetricGap = 10;
    public const double MetricStackHeight = MetricCellHeight * 4 + MetricGap * 3;
    public static double BodyTop => (H - Length) / 2 + Flare;
    public static double BodyBottom => (H + Length) / 2 - Flare;
    public static double ContentWidth => Depth - ContentSidePadding * 2;
    public static double ContentLeft => W - Depth + ContentSidePadding;
    public static double ContentTop => BodyTop + (BodyBottom - BodyTop - MetricStackHeight) / 2;

    public static Geometry Build()
    {
        var top = (H - Length) / 2;
        var bottom = top + Length;
        var right = W;
        var left = right - Depth;
        var bodyTop = top + Flare;
        var bodyBottom = bottom - Flare;
        var g = new StreamGeometry();
        using var c = g.Open();
        c.BeginFigure(new Point(right, top), true);
        c.ArcTo(new Point(right - Flare, bodyTop), new Size(Flare, Flare), 0, false, SweepDirection.Clockwise, true);
        c.LineTo(new Point(left + Corner, bodyTop), true);
        c.ArcTo(new Point(left, bodyTop + Corner), new Size(Corner, Corner), 0, false, SweepDirection.CounterClockwise, true);
        c.LineTo(new Point(left, bodyBottom - Corner), true);
        c.ArcTo(new Point(left + Corner, bodyBottom), new Size(Corner, Corner), 0, false, SweepDirection.CounterClockwise, true);
        c.LineTo(new Point(right - Flare, bodyBottom), true);
        c.ArcTo(new Point(right, bottom), new Size(Flare, Flare), 0, false, SweepDirection.Clockwise, true);
        c.LineTo(new Point(right, top), true);
        c.EndFigure(true);
        return g;
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
            var dy = y - (bodyTop + Corner);
            return left + Corner - Math.Sqrt(Math.Max(0, Corner * Corner - dy * dy));
        }
        if (y > bodyBottom - Corner && y <= bodyBottom)
        {
            var dy = y - (bodyBottom - Corner);
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

internal sealed class VisualNotchWindow : Window
{
    private InputOverlayWindow? _input;
    private readonly TextBlock _status;

    public VisualNotchWindow(string mode)
    {
        Width = NotchShape.W; Height = NotchShape.H;
        CanResize = false; WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false; Topmost = true; ShowActivated = false;
        Background = Brushes.Transparent; TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        RequestedThemeVariant = ThemeVariant.Dark;

        var geometry = NotchShape.Build();
        var canvas = BuildContent(geometry, mode, out _status);
        InsertMaterial(canvas, geometry, mode);
        Content = canvas;

        Opened += (_, _) =>
        {
            PlaceAtRightEdge();
            if (!OperatingSystem.IsWindows()) { _status.Text = "Windows-only"; return; }
            var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero || !Win32.MakeLayeredClickThrough(hwnd))
            { _status.Text = "visual click-through FAILED"; return; }
            _input = new InputOverlayWindow(this, () => _status.Text = $"notch input OK {DateTime.Now:HH:mm:ss}");
            _input.Show();
            _status.Text = "pass-through + shaped input active";
        };
        PositionChanged += (_, _) => { if (_input is not null) _input.Position = Position; };
        Closed += (_, _) => _input?.Close();
    }

    private void PlaceAtRightEdge()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;
        var a = screen.WorkingArea;
        Position = new PixelPoint(a.Right - (int)Math.Round(NotchShape.W * RenderScaling),
            a.Y + (a.Height - (int)Math.Round(NotchShape.H * RenderScaling)) / 2);
    }

    private static void InsertMaterial(Canvas canvas, Geometry geometry, string mode)
    {
        if (mode == "acrylic")
        {
            canvas.Children.Insert(0, new ExperimentalAcrylicBorder
            {
                Width = NotchShape.W, Height = NotchShape.H, Clip = geometry,
                IsHitTestVisible = false,
                Material = new ExperimentalAcrylicMaterial
                {
                    BackgroundSource = AcrylicBackgroundSource.Digger,
                    TintColor = Color.Parse("#090B0F"), TintOpacity = 0.50,
                    MaterialOpacity = 0.30, FallbackColor = Color.Parse("#24272D")
                }
            });
            canvas.Children.Insert(1, Shape(geometry, Color.FromArgb(0x18, 0, 0, 0), Color.FromArgb(0x90, 255, 255, 255)));
        }
        else if (mode == "mica")
            canvas.Children.Insert(0, Shape(geometry, Color.Parse("#CC2B2D31"), Color.FromArgb(0x72, 255, 255, 255)));
        else
            canvas.Children.Insert(0, Shape(geometry, Color.Parse("#050608"), Color.FromArgb(0x30, 255, 255, 255)));
    }

    private static Path Shape(Geometry g, Color fill, Color stroke) => new()
    { Data = g, Fill = new SolidColorBrush(fill), Stroke = new SolidColorBrush(stroke), StrokeThickness = 1, IsHitTestVisible = false };

    private static Canvas BuildContent(Geometry geometry, string mode, out TextBlock status)
    {
        var canvas = new Canvas { Width = NotchShape.W, Height = NotchShape.H, Background = Brushes.Transparent, Clip = geometry };
        TextOptions.SetTextRenderingMode(canvas, TextRenderingMode.Antialias);
        TextOptions.SetTextHintingMode(canvas, TextHintingMode.Strong);
        TextOptions.SetBaselinePixelAlignment(canvas, BaselinePixelAlignment.Aligned);

        var stack = new StackPanel
        {
            Width = NotchShape.ContentWidth,
            Height = NotchShape.MetricStackHeight,
            Spacing = NotchShape.MetricGap,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(Metric("C", "4%", "CPU"));
        stack.Children.Add(Metric("M", "47%", "RAM"));
        stack.Children.Add(Metric("D", "81%", "DISCO"));
        stack.Children.Add(Metric("↕", "SÌ", "RETE"));
        Canvas.SetLeft(stack, NotchShape.ContentLeft);
        Canvas.SetTop(stack, NotchShape.ContentTop);
        canvas.Children.Add(stack);

        status = new TextBlock { Text = mode, FontSize = 7, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#FFB071")), IsHitTestVisible = false };
        Canvas.SetLeft(status, NotchShape.W - NotchShape.Depth + 8);
        Canvas.SetTop(status, (NotchShape.H + NotchShape.Length) / 2 - 20);
        canvas.Children.Add(status);
        return canvas;
    }

    private static Control Metric(string glyph, string value, string caption)
    {
        var ring = new Border
        {
            Width = 46, Height = 46, CornerRadius = new CornerRadius(23), BorderThickness = new Thickness(3),
            BorderBrush = new SolidColorBrush(Color.Parse("#F2F4F7")), Background = new SolidColorBrush(Color.FromArgb(0x28, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock { Text = glyph, FontSize = 16, FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };

        var content = new StackPanel
        {
            Width = NotchShape.ContentWidth,
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                ring,
                new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = caption, FontSize = 8, FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#D4DAE3")), HorizontalAlignment = HorizontalAlignment.Center }
            }
        };

        return new Border
        {
            Width = NotchShape.ContentWidth,
            Height = NotchShape.MetricCellHeight,
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = content
        };
    }
}

internal sealed class InputOverlayWindow : Window
{
    private readonly VisualNotchWindow _visual;
    private readonly Action _clicked;
    public InputOverlayWindow(VisualNotchWindow visual, Action clicked)
    {
        _visual = visual; _clicked = clicked;
        Width = NotchShape.W; Height = NotchShape.H;
        CanResize = false; WindowDecorations = WindowDecorations.None; ShowInTaskbar = false;
        Topmost = true; ShowActivated = false; Background = Brushes.Transparent;
        TransparencyBackgroundFallback = Brushes.Transparent; TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Opacity = 0.001; Content = new Canvas { Background = Brushes.Transparent };
        Opened += (_, _) => { Position = _visual.Position; ApplyInputRegion(); };
        PointerPressed += (_, e) =>
        {
            var p = e.GetCurrentPoint(this);
            if (p.Properties.IsLeftButtonPressed || p.Properties.IsRightButtonPressed)
            { _clicked(); e.Handled = true; }
        };
    }

    private void ApplyInputRegion()
    {
        if (!OperatingSystem.IsWindows()) return;
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) return;
        var s = RenderScaling <= 0 ? 1 : RenderScaling;
        var top = (int)Math.Ceiling(((NotchShape.H - NotchShape.Length) / 2) * s);
        var bottom = (int)Math.Floor(((NotchShape.H + NotchShape.Length) / 2) * s);
        var right = (int)Math.Ceiling(NotchShape.W * s);
        var region = Win32.CreateRectRgn(0, 0, 0, 0);
        if (region == IntPtr.Zero) return;
        var success = false;
        try
        {
            for (var y = top; y < bottom; y++)
            {
                var yDip = (y + 0.5) / s;
                var left = (int)Math.Ceiling(NotchShape.LeftAt(yDip) * s);
                if (left >= right) continue;
                var strip = Win32.CreateRectRgn(left, y, right, y + 1);
                if (strip == IntPtr.Zero) continue;
                try { _ = Win32.CombineRgn(region, region, strip, 2); }
                finally { _ = Win32.DeleteObject(strip); }
            }
            success = Win32.SetWindowRgn(hwnd, region, true) != 0;
        }
        finally { if (!success) _ = Win32.DeleteObject(region); }
    }
}

internal static class Win32
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20L, WsExLayered = 0x80000L, WsExNoActivate = 0x08000000L;
    private const uint SwpNoSize = 0x1, SwpNoMove = 0x2, SwpNoZOrder = 0x4, SwpFrameChanged = 0x20;

    public static bool MakeLayeredClickThrough(IntPtr hwnd)
    {
        var current = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        var desired = current | WsExLayered | WsExTransparent | WsExNoActivate;
        _ = SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(desired));
        _ = SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
        var verify = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        return (verify & WsExLayered) != 0 && (verify & WsExTransparent) != 0 && (verify & WsExNoActivate) != 0;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")] internal static extern int CombineRgn(IntPtr dest, IntPtr src1, IntPtr src2, int mode);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")] internal static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);
}
