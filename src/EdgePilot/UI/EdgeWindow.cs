using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using EdgePilot.Core;
using EdgePilot.Core.Monitoring;
using EdgePilot.Platform;

namespace EdgePilot.UI;

public sealed class EdgeWindow : Window
{
    private const double CollapsedWidth = 12;
    private const double CollapsedHeight = 82;
    private const double ExpandedWidth = 352;
    private const double ExpandedHeight = 508;
    private const double FoldDelayMs = 450;

    private readonly EdgeSide _edge = EdgePlacement.FromEnvironment();
    private readonly SystemMonitorService _monitor = new(new SystemMetricsProvider());
    private readonly CancellationTokenSource _lifetime = new();

    private readonly Border _root;
    private readonly Control _collapsedView;
    private readonly Control _expandedView;
    private readonly TextBlock _hostText;
    private readonly TextBlock _osText;
    private readonly TextBlock _cpuValue;
    private readonly TextBlock _cpuDetail;
    private readonly ProgressBar _cpuBar;
    private readonly TextBlock _ramValue;
    private readonly TextBlock _ramDetail;
    private readonly ProgressBar _ramBar;
    private readonly Border _networkDot;
    private readonly TextBlock _networkName;
    private readonly TextBlock _networkSpeed;
    private readonly TextBlock _uptimeText;
    private readonly TextBlock _updatedText;
    private readonly StackPanel _drivePanel;
    private readonly Button _pinButton;

    private CancellationTokenSource? _foldDelay;
    private bool _expanded;
    private bool _pinned;

    public EdgeWindow()
    {
        Title = "EdgePilot";
        Width = CollapsedWidth;
        Height = CollapsedHeight;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        TransparencyBackgroundFallback = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];

        _hostText = Text("EdgePilot", 18, FontWeight.SemiBold, "#F5F7FA");
        _osText = Text("Starting system monitor…", 11, FontWeight.Normal, "#8B93A1");
        _cpuValue = Text("--%", 30, FontWeight.SemiBold, "#F5F7FA");
        _cpuDetail = Text("CPU", 11, FontWeight.Normal, "#8B93A1");
        _cpuBar = Meter();
        _ramValue = Text("--%", 30, FontWeight.SemiBold, "#F5F7FA");
        _ramDetail = Text("Memory", 11, FontWeight.Normal, "#8B93A1");
        _ramBar = Meter();
        _networkDot = StatusDot(false);
        _networkName = Text("Network", 12, FontWeight.SemiBold, "#F5F7FA");
        _networkSpeed = Text("Waiting for sample…", 11, FontWeight.Normal, "#8B93A1");
        _uptimeText = Text("--", 12, FontWeight.SemiBold, "#F5F7FA");
        _updatedText = Text("--", 10, FontWeight.Normal, "#646C78");
        _drivePanel = new StackPanel { Spacing = 10 };
        _pinButton = SmallButton("PIN");

        _collapsedView = BuildCollapsedView();
        _expandedView = BuildExpandedView();
        _expandedView.IsVisible = false;

        var content = new Grid();
        content.Children.Add(_collapsedView);
        content.Children.Add(_expandedView);

        _root = new Border
        {
            Background = Brush("#050608"),
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = CornerRadiusForEdge(_edge, 8),
            Padding = new Thickness(0),
            Child = content
        };

        Content = _root;

        PointerEntered += (_, _) => Expand();
        PointerExited += (_, _) => ScheduleFold();
        _pinButton.Click += (_, _) => TogglePin();

        _monitor.SnapshotUpdated += OnSnapshotUpdated;
        _monitor.CaptureFailed += OnCaptureFailed;

        Opened += OnOpened;
        Closed += OnClosed;
        ScalingChanged += (_, _) => Relocate();
    }

    private Control BuildCollapsedView()
    {
        return new Border
        {
            Background = Brush("#050608"),
            CornerRadius = CornerRadiusForEdge(_edge, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    private Control BuildExpandedView()
    {
        var main = new StackPanel { Spacing = 14 };

        var header = new DockPanel { LastChildFill = true };
        var headerButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var quitButton = SmallButton("×");
        quitButton.Width = 34;
        quitButton.Click += (_, _) => Close();
        headerButtons.Children.Add(_pinButton);
        headerButtons.Children.Add(quitButton);
        DockPanel.SetDock(headerButtons, Dock.Right);
        header.Children.Add(headerButtons);

        var identity = new StackPanel { Spacing = 2 };
        identity.Children.Add(_hostText);
        identity.Children.Add(_osText);
        header.Children.Add(identity);
        main.Children.Add(header);

        var metrics = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        metrics.Children.Add(MetricCard("CPU", _cpuValue, _cpuDetail, _cpuBar));
        metrics.Children.Add(MetricCard("MEMORY", _ramValue, _ramDetail, _ramBar));
        main.Children.Add(metrics);

        main.Children.Add(SectionLabel("NETWORK"));
        var networkRow = new StackPanel { Spacing = 5 };
        var networkTitle = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        networkTitle.Children.Add(_networkDot);
        networkTitle.Children.Add(_networkName);
        networkRow.Children.Add(networkTitle);
        networkRow.Children.Add(_networkSpeed);
        main.Children.Add(Card(networkRow));

        var uptimeRow = new DockPanel();
        var uptimeLabel = Text("Uptime", 11, FontWeight.Normal, "#8B93A1");
        DockPanel.SetDock(uptimeLabel, Dock.Left);
        DockPanel.SetDock(_uptimeText, Dock.Right);
        uptimeRow.Children.Add(uptimeLabel);
        uptimeRow.Children.Add(_uptimeText);
        main.Children.Add(Card(uptimeRow));

        main.Children.Add(SectionLabel("STORAGE"));
        main.Children.Add(_drivePanel);

        var footer = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
        var edgeText = Text($"{_edge.ToString().ToUpperInvariant()} EDGE · v0.1", 9, FontWeight.SemiBold, "#535B67");
        DockPanel.SetDock(edgeText, Dock.Left);
        DockPanel.SetDock(_updatedText, Dock.Right);
        footer.Children.Add(edgeText);
        footer.Children.Add(_updatedText);
        main.Children.Add(footer);

        return main;
    }

    private static Border MetricCard(string label, TextBlock value, TextBlock detail, ProgressBar bar)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(Text(label, 9, FontWeight.Bold, "#6E7683"));
        stack.Children.Add(value);
        stack.Children.Add(detail);
        stack.Children.Add(bar);

        return new Border
        {
            Width = 153,
            Background = Brush("#11151B"),
            BorderBrush = Brush("#242A33"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Child = stack
        };
    }

    private static Border Card(Control child) => new()
    {
        Background = Brush("#0F1318"),
        BorderBrush = Brush("#222832"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(12),
        Child = child
    };

    private static ProgressBar Meter() => new()
    {
        Minimum = 0,
        Maximum = 100,
        Value = 0,
        Height = 5,
        Foreground = Brush("#DDE3EA"),
        Background = Brush("#252B34")
    };

    private static Button SmallButton(string text) => new()
    {
        Content = text,
        Height = 30,
        MinWidth = 48,
        Padding = new Thickness(9, 3),
        FontSize = 10,
        FontWeight = FontWeight.SemiBold,
        Background = Brush("#171C23"),
        Foreground = Brush("#C9D0D8"),
        BorderBrush = Brush("#2C333E")
    };

    private static TextBlock SectionLabel(string text) => Text(text, 9, FontWeight.Bold, "#68717E");

    private static TextBlock Text(
        string value,
        double size,
        FontWeight weight,
        string color,
        TextAlignment alignment = TextAlignment.Left) => new()
    {
        Text = value,
        FontSize = size,
        FontWeight = weight,
        Foreground = Brush(color),
        TextAlignment = alignment,
        TextWrapping = TextWrapping.NoWrap
    };

    private static Border StatusDot(bool online) => new()
    {
        Width = 8,
        Height = 8,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        CornerRadius = new CornerRadius(4),
        Background = Brush(online ? "#4AE39A" : "#59616D")
    };

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private void OnOpened(object? sender, EventArgs e)
    {
        Relocate();
        Screens.Changed += OnScreensChanged;
        _ = Task.Run(() => _monitor.RunAsync(_lifetime.Token));
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _foldDelay?.Cancel();
        _lifetime.Cancel();
        Screens.Changed -= OnScreensChanged;
        _monitor.SnapshotUpdated -= OnSnapshotUpdated;
        _monitor.CaptureFailed -= OnCaptureFailed;
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
            _osText.Text = $"Monitor error · {exception.GetType().Name}";
        });
    }

    private void RenderSnapshot(SystemSnapshot snapshot)
    {
        var cpu = Math.Clamp(snapshot.CpuPercent, 0, 100);
        var ram = Math.Clamp(snapshot.MemoryUsedPercent, 0, 100);

        _hostText.Text = snapshot.HostName;
        _osText.Text = snapshot.OperatingSystem;

        _cpuValue.Text = $"{cpu:0}%";
        _cpuDetail.Text = Environment.ProcessorCount == 1 ? "1 logical CPU" : $"{Environment.ProcessorCount} logical CPUs";
        _cpuBar.Value = cpu;

        _ramValue.Text = $"{ram:0}%";
        _ramDetail.Text = snapshot.MemoryTotalBytes > 0
            ? $"{DisplayFormat.Bytes(snapshot.MemoryUsedBytes)} / {DisplayFormat.Bytes(snapshot.MemoryTotalBytes)}"
            : "Memory unavailable";
        _ramBar.Value = ram;

        SetDot(_networkDot, snapshot.Network.Connected);
        _networkName.Text = snapshot.Network.Connected ? snapshot.Network.InterfaceName : "Offline";
        _networkSpeed.Text = snapshot.Network.Connected
            ? $"↓ {DisplayFormat.Rate(snapshot.Network.ReceiveBytesPerSecond)}    ↑ {DisplayFormat.Rate(snapshot.Network.SendBytesPerSecond)}"
            : "No active network interface";

        _uptimeText.Text = DisplayFormat.Uptime(snapshot.Uptime);
        _updatedText.Text = snapshot.CapturedAt.ToString("HH:mm:ss");
        RenderDrives(snapshot.Drives);
    }

    private void RenderDrives(IReadOnlyList<DriveSnapshot> drives)
    {
        _drivePanel.Children.Clear();

        if (drives.Count == 0)
        {
            _drivePanel.Children.Add(Card(Text("No fixed drives detected", 11, FontWeight.Normal, "#8B93A1")));
            return;
        }

        foreach (var drive in drives)
        {
            var titleRow = new DockPanel();
            var name = Text(drive.Label, 11, FontWeight.SemiBold, "#E8ECF1");
            var space = Text($"{DisplayFormat.Bytes(drive.FreeBytes)} free", 10, FontWeight.Normal, "#858E9B");
            DockPanel.SetDock(name, Dock.Left);
            DockPanel.SetDock(space, Dock.Right);
            titleRow.Children.Add(name);
            titleRow.Children.Add(space);

            var bar = Meter();
            bar.Value = Math.Clamp(drive.UsedPercent, 0, 100);

            var stack = new StackPanel { Spacing = 6 };
            stack.Children.Add(titleRow);
            stack.Children.Add(bar);
            _drivePanel.Children.Add(Card(stack));
        }
    }

    private static void SetDot(Border dot, bool online)
    {
        dot.Background = Brush(online ? "#4AE39A" : "#59616D");
    }

    private void Expand()
    {
        _foldDelay?.Cancel();
        if (_expanded) return;
        SetExpanded(true);
    }

    private void ScheduleFold()
    {
        if (_pinned || !_expanded) return;
        _foldDelay?.Cancel();
        _foldDelay?.Dispose();
        _foldDelay = new CancellationTokenSource();
        var token = _foldDelay.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(FoldDelayMs), token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Post(() => SetExpanded(false));
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private void TogglePin()
    {
        _pinned = !_pinned;
        _pinButton.Content = _pinned ? "UNPIN" : "PIN";
        if (_pinned) Expand();
        else ScheduleFold();
    }

    private void SetExpanded(bool expanded)
    {
        if (_expanded == expanded) return;
        _expanded = expanded;
        _collapsedView.IsVisible = !expanded;
        _expandedView.IsVisible = expanded;

        _root.Background = expanded ? Brush("#0B0D11") : Brush("#050608");
        _root.BorderBrush = expanded ? Brush("#252A33") : Brushes.Transparent;
        _root.BorderThickness = expanded ? new Thickness(1) : new Thickness(0);
        _root.CornerRadius = expanded ? CornerRadiusForEdge(_edge, 22) : CornerRadiusForEdge(_edge, 8);
        _root.Padding = expanded ? new Thickness(16) : new Thickness(0);

        Width = expanded ? ExpandedWidth : CollapsedWidth;
        Height = expanded ? ExpandedHeight : CollapsedHeight;
        Relocate();
        Dispatcher.UIThread.Post(Relocate, DispatcherPriority.Background);
    }

    private void Relocate()
    {
        if (!IsVisible) return;
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;

        Position = EdgePlacement.Calculate(screen, _edge, new Size(Width, Height));
    }

    private static CornerRadius CornerRadiusForEdge(EdgeSide edge, double radius) => edge switch
    {
        EdgeSide.Right => new CornerRadius(radius, 0, 0, radius),
        EdgeSide.Left => new CornerRadius(0, radius, radius, 0),
        EdgeSide.Top => new CornerRadius(0, 0, radius, radius),
        EdgeSide.Bottom => new CornerRadius(radius, radius, 0, 0),
        _ => new CornerRadius(radius)
    };
}
