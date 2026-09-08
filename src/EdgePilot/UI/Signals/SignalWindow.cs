using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using EdgePilot.Core.Signals;
using EdgePilot.Platform;

namespace EdgePilot.UI.Signals;

internal sealed class SignalWindow : Window
{
    private const double WindowWidth = NotchLayout.DesignWidth;
    private const double WindowHeight = NotchLayout.DesignHeight;
    private const double CollapsedDepth = 10;
    private const double CollapsedLength = 82;
    private const double SignalDepth = 126;
    private const double SignalLength = 260;

    private readonly Path _shape;
    private readonly Canvas _content;
    private readonly SignalPresenter _presenter;
    private readonly DispatcherTimer _motionTimer = new();
    private readonly NotchSpring _spring = new();
    private readonly Stopwatch _clock = new();
    private readonly Win32Properties.CustomWndProcHookCallback? _wndProcHook;
    private EdgeSide _edge;

    public event Action? Collapsed;

    public SignalWindow(EdgeSide edge)
    {
        _edge = edge;
        Title = "EdgePilot Signal";
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        IsHitTestVisible = false;
        Background = Brushes.Transparent;
        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];

        _shape = new Path
        {
            Fill = Brush("#050608"),
            StrokeThickness = 0,
            IsHitTestVisible = false
        };

        _presenter = new SignalPresenter();
        _content = new Canvas
        {
            IsHitTestVisible = false,
            Opacity = 0
        };
        _content.Children.Add(_presenter);

        var root = new Canvas
        {
            Background = Brushes.Transparent,
            IsHitTestVisible = false
        };
        root.Children.Add(_shape);
        root.Children.Add(_content);
        Content = root;

        ConfigureEdge(edge);
        UpdateVisual();

        _motionTimer.Interval = TimeSpan.FromMilliseconds(16);
        _motionTimer.Tick += (_, _) =>
        {
            _spring.Advance(_clock.Elapsed.TotalSeconds);
            _clock.Restart();
            UpdateVisual();
            if (!_spring.IsSettled) return;

            _motionTimer.Stop();
            if (_spring.Target > 0) return;
            _presenter.Clear();
            Hide();
            Collapsed?.Invoke();
        };

        ScalingChanged += (_, _) => Relocate();
        Opened += (_, _) => Relocate();
        Closed += (_, _) =>
        {
            _motionTimer.Stop();
            if (_wndProcHook is not null)
                Win32Properties.RemoveWndProcHookCallback(this, _wndProcHook);
        };

        if (OperatingSystem.IsWindows())
        {
            _wndProcHook = WndProc;
            Win32Properties.AddWndProcHookCallback(this, _wndProcHook);
        }
    }

    public void ConfigureEdge(EdgeSide edge)
    {
        _edge = edge;
        var size = NotchLayout.WindowSize(edge);
        Width = size.Width;
        Height = size.Height;

        if (Content is Canvas root)
        {
            root.Width = _content.Width = size.Width;
            root.Height = _content.Height = size.Height;
        }

        var design = new Rect(
            WindowWidth - SignalDepth,
            (WindowHeight - SignalLength) / 2,
            SignalDepth,
            SignalLength);
        var bounds = NotchLayout.ToScreen(design, edge);
        var presenterBounds = new Rect(
            bounds.X + 7,
            bounds.Y + 7,
            Math.Max(1, bounds.Width - 14),
            Math.Max(1, bounds.Height - 14));

        _presenter.Configure(edge, presenterBounds);
        Canvas.SetLeft(_presenter, presenterBounds.X);
        Canvas.SetTop(_presenter, presenterBounds.Y);
        Relocate();
        UpdateVisual();
    }

    public void ShowSignal(Signal signal)
    {
        _presenter.Render(signal);
        if (!IsVisible) Show();
        Relocate();
        _spring.Target = 1;
        StartMotion();
    }

    public void HideSignal()
    {
        if (!IsVisible) return;
        _spring.Target = 0;
        StartMotion();
    }

    private void StartMotion()
    {
        _clock.Restart();
        _motionTimer.Start();
    }

    private void UpdateVisual()
    {
        var p = Math.Clamp(_spring.Position, 0, 1.025);
        var depth = Lerp(CollapsedDepth, SignalDepth, p);
        var length = Lerp(CollapsedLength, SignalLength, p);
        var geometry = EdgeNotchGeometry.BuildRight(WindowWidth, WindowHeight, depth, length);
        geometry.Transform = new MatrixTransform(NotchLayout.Transform(_edge));
        _shape.Data = geometry;
        _content.Clip = geometry;

        var contentProgress = Math.Clamp((p - 0.18) / 0.62, 0, 1);
        _content.Opacity = contentProgress;
        _presenter.Opacity = contentProgress;
    }

    private void Relocate()
    {
        if (!IsVisible) return;
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;
        Position = EdgePlacement.Calculate(screen, _edge, new Size(Width, Height));
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const uint WmNcHitTest = 0x0084;
        const int HtTransparent = -1;
        if (msg != WmNcHitTest) return IntPtr.Zero;
        handled = true;
        return new IntPtr(HtTransparent);
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
    private static double Lerp(double from, double to, double amount) => from + (to - from) * amount;
}
