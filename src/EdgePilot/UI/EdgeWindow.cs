using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Path = Avalonia.Controls.Shapes.Path;
using EdgePilot.Core;
using EdgePilot.Core.Monitoring;
using EdgePilot.Platform;

namespace EdgePilot.UI;

public sealed class EdgeWindow : Window
{
    private const double WindowWidth = 410;
    private const double WindowHeight = 620;

    private const double CollapsedDepth = 10;
    private const double CollapsedLength = 82;
    private const double ExpandedDepth = 88;
    private const double ExpandedLength = 430;
    private const double HotZoneDepth = 36;
    private const double HotZoneLength = 120;

    private const double FoldDelayMs = 450;
    private const double MotionDurationMs = 420;

    private readonly EdgeSide _edge = EdgePlacement.FromEnvironment();
    private readonly SystemMonitorService _monitor = new(new SystemMetricsProvider());
    private readonly CancellationTokenSource _lifetime = new();
    private readonly DispatcherTimer _cursorTimer;

    private readonly Path _notchShape;
    private readonly Canvas _notchContent;
    private readonly StackPanel _metricStack;
    private readonly MetricRing _cpuRing;
    private readonly MetricRing _ramRing;
    private readonly MetricRing _diskRing;
    private readonly MetricRing _networkRing;

    private readonly Border _tooltipCard;
    private readonly TextBlock _tooltipTitle;
    private readonly TextBlock _tooltipValue;
    private readonly TextBlock _tooltipLine1;
    private readonly TextBlock _tooltipLine2;
    private readonly TextBlock _tooltipLine3;

    private CancellationTokenSource? _foldDelay;
    private CancellationTokenSource? _motion;
    private SystemSnapshot? _latestSnapshot;
    private double _expansion;
    private bool _expanded;
    private bool _pinned;
    private int? _hoveredMetric;

    private readonly Win32Properties.CustomWndProcHookCallback? _wndProcHook;

    public EdgeWindow()
    {
        Title = "EdgePilot";
        Width = WindowWidth;
        Height = WindowHeight;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];

        _notchShape = new Path
        {
            Fill = Brush("#050608"),
            StrokeThickness = 0,
            IsHitTestVisible = false
        };

        _cpuRing = new MetricRing("C", "CPU");
        _ramRing = new MetricRing("M", "MEM");
        _diskRing = new MetricRing("D", "DISK");
        _networkRing = new MetricRing("↕", "NET");

        _metricStack = new StackPanel
        {
            Width = ExpandedDepth,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _metricStack.Children.Add(_cpuRing);
        _metricStack.Children.Add(_ramRing);
        _metricStack.Children.Add(_diskRing);
        _metricStack.Children.Add(_networkRing);

        _notchContent = new Canvas
        {
            Width = WindowWidth,
            Height = WindowHeight,
            Opacity = 0,
            IsHitTestVisible = false
        };
        _notchContent.Children.Add(_metricStack);
        Canvas.SetLeft(_metricStack, WindowWidth - ExpandedDepth);
        Canvas.SetTop(_metricStack, (WindowHeight - 350) / 2);

        _tooltipTitle = Text("CPU", 10, FontWeight.Bold, "#858E9B");
        _tooltipValue = Text("—", 28, FontWeight.SemiBold, "#F5F7FA");
        _tooltipLine1 = Text("", 11, FontWeight.Normal, "#C9D0D8");
        _tooltipLine2 = Text("", 11, FontWeight.Normal, "#8B93A1");
        _tooltipLine3 = Text("", 10, FontWeight.Normal, "#68717E");

        var tooltipStack = new StackPanel { Spacing = 7 };
        tooltipStack.Children.Add(_tooltipTitle);
        tooltipStack.Children.Add(_tooltipValue);
        tooltipStack.Children.Add(_tooltipLine1);
        tooltipStack.Children.Add(_tooltipLine2);
        tooltipStack.Children.Add(_tooltipLine3);

        _tooltipCard = new Border
        {
            Width = 270,
            MinHeight = 148,
            Padding = new Thickness(16),
            Background = Brush("#101318"),
            BorderBrush = Brush("#2A3039"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Child = tooltipStack,
            IsVisible = false,
            Opacity = 0
        };

        var root = new Canvas
        {
            Width = WindowWidth,
            Height = WindowHeight
        };
        root.Children.Add(_notchShape);
        root.Children.Add(_notchContent);
        root.Children.Add(_tooltipCard);
        Content = root;

        UpdateNotchVisual();

        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;

        _monitor.SnapshotUpdated += OnSnapshotUpdated;
        _monitor.CaptureFailed += OnCaptureFailed;

        Opened += OnOpened;
        Closed += OnClosed;
        ScalingChanged += (_, _) => Relocate();

        _cursorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _cursorTimer.Tick += (_, _) => PollCursor();

        if (OperatingSystem.IsWindows())
        {
            _wndProcHook = WndProc;
            Win32Properties.AddWndProcHookCallback(this, _wndProcHook);
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Relocate();
        Screens.Changed += OnScreensChanged;
        _cursorTimer.Start();
        _ = Task.Run(() => _monitor.RunAsync(_lifetime.Token));
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _cursorTimer.Stop();
        _foldDelay?.Cancel();
        _motion?.Cancel();
        _lifetime.Cancel();
        Screens.Changed -= OnScreensChanged;
        _monitor.SnapshotUpdated -= OnSnapshotUpdated;
        _monitor.CaptureFailed -= OnCaptureFailed;

        if (_wndProcHook is not null)
            Win32Properties.RemoveWndProcHookCallback(this, _wndProcHook);

        _foldDelay?.Dispose();
        _motion?.Dispose();
        _lifetime.Dispose();
    }

    private void OnScreensChanged(object? sender, EventArgs e) => Relocate();

    private void OnSnapshotUpdated(SystemSnapshot snapshot)
    {
        Dispatcher.UIThread.Post(() => RenderSnapshot(snapshot));
    }

    private void OnCaptureFailed(Exception exception)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _tooltipTitle.Text = "SYSTEM MONITOR";
            _tooltipValue.Text = "ERROR";
            _tooltipLine1.Text = exception.GetType().Name;
        });
    }

    private void RenderSnapshot(SystemSnapshot snapshot)
    {
        _latestSnapshot = snapshot;

        var cpu = Math.Clamp(snapshot.CpuPercent, 0, 100);
        var ram = Math.Clamp(snapshot.MemoryUsedPercent, 0, 100);
        var drive = snapshot.Drives.FirstOrDefault();

        _cpuRing.SetValue(cpu, $"{cpu:0}%");
        _ramRing.SetValue(ram, $"{ram:0}%");
        _diskRing.SetValue(drive?.UsedPercent, drive is null ? "—" : $"{drive.UsedPercent:0}%");
        _networkRing.SetValue(null, snapshot.Network.Connected ? "ON" : "OFF");

        if (_hoveredMetric is not null)
            RenderTooltip(_hoveredMetric.Value);
    }

    private void PollCursor()
    {
        if (!TryGetCursorLocal(out var point))
            return;

        if (!_expanded)
        {
            SetHoveredMetric(null);
            if (HotZoneRect().Contains(point))
                Expand();
            return;
        }

        var hovered = MetricIndexAt(point);
        if (hovered is not null)
            SetHoveredMetric(hovered);
        else if (!TooltipLiveRect().Contains(point))
            SetHoveredMetric(null);

        if (ExpandedLiveRect().Contains(point) || TooltipLiveRect().Contains(point) || BridgeRect().Contains(point))
        {
            CancelFold();
        }
        else
        {
            ScheduleFold();
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        if (!_expanded)
        {
            if (HotZoneRect().Contains(point))
                Expand();
            return;
        }

        var hovered = MetricIndexAt(point);
        if (hovered is not null)
            SetHoveredMetric(hovered);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var point = e.GetPosition(this);
        if (!ShapeRect().Contains(point))
            return;

        if (!_expanded)
        {
            Expand();
            return;
        }

        _pinned = !_pinned;
        if (_pinned)
            CancelFold();
        else
            ScheduleFold();
    }

    private void Expand()
    {
        CancelFold();
        if (_expanded) return;
        _expanded = true;
        _ = AnimateExpansionAsync(1);
    }

    private void ScheduleFold()
    {
        if (_pinned || !_expanded || _foldDelay is not null)
            return;

        _foldDelay = new CancellationTokenSource();
        var token = _foldDelay.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(FoldDelayMs), token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (_pinned) return;
                        _expanded = false;
                        SetHoveredMetric(null);
                        _ = AnimateExpansionAsync(0);
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _foldDelay?.Dispose();
                    _foldDelay = null;
                });
            }
        }, token);
    }

    private void CancelFold()
    {
        _foldDelay?.Cancel();
    }

    private async Task AnimateExpansionAsync(double target)
    {
        _motion?.Cancel();
        _motion?.Dispose();
        _motion = new CancellationTokenSource();
        var token = _motion.Token;
        var start = _expansion;
        var started = DateTime.UtcNow;

        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
                var time = Math.Clamp(elapsed / MotionDurationMs, 0, 1);
                var eased = Springish(time);
                _expansion = Lerp(start, target, eased);
                _expansion = Math.Clamp(_expansion, -0.02, 1.04);
                UpdateNotchVisual();

                if (time >= 1)
                    break;

                await Task.Delay(16, token);
            }

            _expansion = target;
            UpdateNotchVisual();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void UpdateNotchVisual()
    {
        var p = Math.Clamp(_expansion, 0, 1);
        var depth = Lerp(CollapsedDepth, ExpandedDepth, p);
        var length = Lerp(CollapsedLength, ExpandedLength, p);
        var geometry = EdgeNotchGeometry.BuildRight(WindowWidth, WindowHeight, depth, length);

        _notchShape.Data = geometry;
        _notchContent.Clip = geometry;

        var contentProgress = Math.Clamp((p - 0.16) / 0.72, 0, 1);
        _notchContent.Opacity = contentProgress;
        _metricStack.RenderTransform = new TranslateTransform(12 * (1 - contentProgress), 0);

        if (p < 0.78)
        {
            _tooltipCard.Opacity = 0;
            if (p < 0.5)
                _tooltipCard.IsVisible = false;
        }
        else if (_hoveredMetric is not null)
        {
            _tooltipCard.IsVisible = true;
            _tooltipCard.Opacity = Math.Clamp((p - 0.78) / 0.22, 0, 1);
        }
    }

    private int? MetricIndexAt(Point point)
    {
        if (_expansion < 0.82)
            return null;

        var xMin = WindowWidth - ExpandedDepth;
        if (point.X < xMin || point.X > WindowWidth)
            return null;

        var stackTop = (WindowHeight - 350) / 2;
        const double cell = 75;
        const double gap = 12;

        for (var index = 0; index < 4; index++)
        {
            var top = stackTop + index * (cell + gap);
            if (point.Y >= top && point.Y <= top + cell)
                return index;
        }

        return null;
    }

    private void SetHoveredMetric(int? index)
    {
        if (_hoveredMetric == index)
            return;

        _hoveredMetric = index;
        if (index is null || !_expanded || _expansion < 0.78)
        {
            _tooltipCard.Opacity = 0;
            _tooltipCard.IsVisible = false;
            return;
        }

        RenderTooltip(index.Value);
        PositionTooltip(index.Value);
        _tooltipCard.IsVisible = true;
        _tooltipCard.Opacity = 1;
    }

    private void RenderTooltip(int index)
    {
        var snapshot = _latestSnapshot;
        if (snapshot is null)
            return;

        var cpu = Math.Clamp(snapshot.CpuPercent, 0, 100);
        var ram = Math.Clamp(snapshot.MemoryUsedPercent, 0, 100);
        var drive = snapshot.Drives.FirstOrDefault();

        switch (index)
        {
            case 0:
                _tooltipTitle.Text = "CPU";
                _tooltipValue.Text = $"{cpu:0}%";
                _tooltipLine1.Text = $"{Environment.ProcessorCount} logical processors";
                _tooltipLine2.Text = snapshot.HostName;
                _tooltipLine3.Text = snapshot.OperatingSystem;
                break;

            case 1:
                _tooltipTitle.Text = "MEMORY";
                _tooltipValue.Text = $"{ram:0}%";
                _tooltipLine1.Text = $"{DisplayFormat.Bytes(snapshot.MemoryUsedBytes)} used";
                _tooltipLine2.Text = $"{DisplayFormat.Bytes(snapshot.MemoryAvailableBytes)} available";
                _tooltipLine3.Text = $"{DisplayFormat.Bytes(snapshot.MemoryTotalBytes)} total";
                break;

            case 2:
                _tooltipTitle.Text = "STORAGE";
                if (drive is null)
                {
                    _tooltipValue.Text = "—";
                    _tooltipLine1.Text = "No fixed drive detected";
                    _tooltipLine2.Text = "";
                    _tooltipLine3.Text = "";
                }
                else
                {
                    _tooltipValue.Text = $"{drive.UsedPercent:0}%";
                    _tooltipLine1.Text = drive.Label;
                    _tooltipLine2.Text = $"{DisplayFormat.Bytes(drive.FreeBytes)} free";
                    _tooltipLine3.Text = $"{DisplayFormat.Bytes(drive.TotalBytes)} total";
                }
                break;

            default:
                _tooltipTitle.Text = "NETWORK";
                _tooltipValue.Text = snapshot.Network.Connected ? "ONLINE" : "OFFLINE";
                _tooltipLine1.Text = snapshot.Network.Connected ? snapshot.Network.InterfaceName : "No active interface";
                _tooltipLine2.Text = snapshot.Network.Connected
                    ? $"↓ {DisplayFormat.Rate(snapshot.Network.ReceiveBytesPerSecond)}   ↑ {DisplayFormat.Rate(snapshot.Network.SendBytesPerSecond)}"
                    : "";
                _tooltipLine3.Text = snapshot.Network.LinkSpeedBitsPerSecond > 0
                    ? $"Link {snapshot.Network.LinkSpeedBitsPerSecond / 1_000_000d:0} Mbps"
                    : $"Uptime {DisplayFormat.Uptime(snapshot.Uptime)}";
                break;
        }
    }

    private void PositionTooltip(int index)
    {
        const double tooltipWidth = 270;
        const double gap = 16;
        var x = WindowWidth - ExpandedDepth - gap - tooltipWidth;
        var stackTop = (WindowHeight - 350) / 2;
        var cellCenter = stackTop + index * 87 + 37;
        var y = Math.Clamp(cellCenter - 82, 24, WindowHeight - 190);
        Canvas.SetLeft(_tooltipCard, x);
        Canvas.SetTop(_tooltipCard, y);
    }

    private Rect HotZoneRect()
    {
        return new Rect(
            WindowWidth - HotZoneDepth,
            (WindowHeight - HotZoneLength) / 2,
            HotZoneDepth,
            HotZoneLength);
    }

    private Rect ShapeRect()
    {
        var p = Math.Clamp(_expansion, 0, 1);
        var depth = Lerp(CollapsedDepth, ExpandedDepth, p);
        var length = Lerp(CollapsedLength, ExpandedLength, p);
        return new Rect(WindowWidth - depth, (WindowHeight - length) / 2, depth, length);
    }

    private Rect ExpandedLiveRect() => ShapeRect();

    private Rect TooltipLiveRect()
    {
        if (!_tooltipCard.IsVisible || _hoveredMetric is null)
            return Rect.Empty;

        var x = Canvas.GetLeft(_tooltipCard);
        var y = Canvas.GetTop(_tooltipCard);
        return new Rect(x, y, _tooltipCard.Width, Math.Max(_tooltipCard.Bounds.Height, 170));
    }

    private Rect BridgeRect()
    {
        if (_hoveredMetric is null || !_tooltipCard.IsVisible)
            return Rect.Empty;

        var tip = TooltipLiveRect();
        var shape = ShapeRect();
        var y = tip.Y + tip.Height / 2 - 30;
        return new Rect(tip.Right, y, Math.Max(0, shape.Left - tip.Right), 60);
    }

    private bool IsInteractive(Point point)
    {
        if (!_expanded)
            return ShapeRect().Contains(point);

        return ExpandedLiveRect().Contains(point)
            || TooltipLiveRect().Contains(point)
            || BridgeRect().Contains(point);
    }

    private bool TryGetCursorLocal(out Point point)
    {
        if (OperatingSystem.IsWindows() && GetCursorPos(out var cursor))
        {
            var scaling = RenderScaling <= 0 ? 1 : RenderScaling;
            point = new Point(
                (cursor.X - Position.X) / scaling,
                (cursor.Y - Position.Y) / scaling);
            return true;
        }

        point = default;
        return false;
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const uint WmNcHitTest = 0x0084;
        const int HtTransparent = -1;
        const int HtClient = 1;

        if (msg != WmNcHitTest || !GetCursorPos(out var cursor))
            return IntPtr.Zero;

        var scaling = RenderScaling <= 0 ? 1 : RenderScaling;
        var point = new Point(
            (cursor.X - Position.X) / scaling,
            (cursor.Y - Position.Y) / scaling);

        handled = true;
        return new IntPtr(IsInteractive(point) ? HtClient : HtTransparent);
    }

    private void Relocate()
    {
        if (!IsVisible) return;
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;

        Position = EdgePlacement.Calculate(screen, _edge, new Size(Width, Height));
    }

    private static TextBlock Text(string value, double size, FontWeight weight, string color) => new()
    {
        Text = value,
        FontSize = size,
        FontWeight = weight,
        Foreground = Brush(color),
        TextWrapping = TextWrapping.Wrap
    };

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private static double Lerp(double from, double to, double amount) => from + (to - from) * amount;

    private static double Springish(double t)
    {
        const double c1 = 1.15;
        var c3 = c1 + 1;
        var x = t - 1;
        return 1 + c3 * x * x * x + c1 * x * x;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);
}
