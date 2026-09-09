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
    private double ExpandedLength => StackHeight + 66;
    private double HotZoneDepth => _sensitivity switch { HoverSensitivity.Precise => 18, HoverSensitivity.Wide => 54, _ => 36 };
    private double HotZoneLength => _sensitivity switch { HoverSensitivity.Precise => 96, HoverSensitivity.Wide => 152, _ => 120 };

    private const double FoldDelayMs = 450;
    private const double CellHeight = 78;
    private const double CellGap = 10;
    private int[] _visibleMetricIndices = [0, 1, 2, 3];
    private double StackHeight => _visibleMetricIndices.Length * CellHeight + (_visibleMetricIndices.Length - 1) * CellGap;
    private HoverSensitivity _sensitivity = HoverSensitivity.Normal;
    private VisibleMetrics _metrics = VisibleMetrics.All;

    private EdgeSide _edge = EdgePlacement.FromEnvironment();
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

    private readonly DispatcherTimer _foldTimer = new();
    private readonly DispatcherTimer _motionTimer = new();
    private readonly NotchSpring _spring = new();
    private readonly System.Diagnostics.Stopwatch _motionClock = new();
    private SystemSnapshot? _latestSnapshot;
    private double _expansion;
    private bool _expanded;
    private bool _pinned;
    private bool _screensSubscribed;
    private bool _started;
    private NotchDisplayMode _mode;
    private SettingsBackdrop _backdrop = SettingsBackdrop.Flat;
    private SettingsThemePreference _settingsTheme = SettingsThemePreference.System;
    private string? _selectedDrive;
    private bool _startAtLogin;
    private Language? _language;
    public bool HasTray { get; set; }
    public event Action? ExitRequested;
    public event Action? PreferencesChanged;
    public Action<NotchPreferences>? SavePreferences { get; set; }
    private SettingsWindow? _settingsWindow;
    private int? _hoveredMetric;

    private readonly PlatformInputRegion _inputRegion;

    public EdgeWindow()
    {
        Title = "EdgePilot";
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
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

        _inputRegion = new PlatformInputRegion(this);

        _notchShape = new Path
        {
            Fill = Brush("#050608"),
            StrokeThickness = 0,
            IsHitTestVisible = false
        };

        _cpuRing = new MetricRing("C", "CPU");
        _ramRing = new MetricRing("M", "RAM");
        _diskRing = new MetricRing("D", Localization.T("ring.disk"));
        _networkRing = new MetricRing("↕", Localization.T("ring.network"));

        _metricStack = new StackPanel
        {
            Width = ExpandedDepth,
            Spacing = CellGap,
            Height = StackHeight,
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
        Canvas.SetTop(_metricStack, (WindowHeight - StackHeight) / 2);

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
            MinHeight = 174,
            Padding = new Thickness(16),
            Background = Brush("#101318"),
            BorderBrush = Brush("#2A3039"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Child = tooltipStack,
            IsVisible = false,
            Opacity = 0
        };

        var root = new Canvas
        {
            Background = Brushes.Transparent,
            Width = WindowWidth,
            Height = WindowHeight
        };
        root.Children.Add(_notchShape);
        root.Children.Add(_notchContent);
        root.Children.Add(_tooltipCard);
        Content = root;

        ConfigureLayout();
        UpdateNotchVisual();

        PointerMoved += OnPointerMoved;
        PointerExited += (_, _) => ScheduleFold();
        _foldTimer.Interval = TimeSpan.FromMilliseconds(FoldDelayMs);
        _foldTimer.Tick += (_, _) =>
        {
            _foldTimer.Stop();
            if (_pinned || !_expanded) return;
            _expanded = false;
            SetHoveredMetric(null);
            StartMotion(0);
        };
        _motionTimer.Interval = TimeSpan.FromMilliseconds(16);
        _motionTimer.Tick += (_, _) =>
        {
            _spring.Advance(_motionClock.Elapsed.TotalSeconds);
            _motionClock.Restart();
            _expansion = _spring.Position;
            UpdateNotchVisual();
            if (_spring.IsSettled) _motionTimer.Stop();
        };
        PointerPressed += OnPointerPressed;

        _monitor.SnapshotUpdated += OnSnapshotUpdated;
        _monitor.CaptureFailed += OnCaptureFailed;

        Opened += OnOpened;
        Closed += OnClosed;
        ScalingChanged += (_, _) =>
        {
            Relocate();
            UpdatePlatformInputRegion();
        };

        _cursorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _cursorTimer.Tick += (_, _) => PollCursor();
    }

    internal bool HasSafePlatformInput =>
        (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux()) || _inputRegion.IsReady;

    private void OnOpened(object? sender, EventArgs e)
    {
        Relocate();
        if (_started) return;
        _started = true;
        Screens.Changed += OnScreensChanged;
        _screensSubscribed = true;
        _ = Task.Run(() => _monitor.RunAsync(_lifetime.Token));
        if (_mode == NotchDisplayMode.Hidden)
        {
            _cursorTimer.Stop();
            Hide();
        }
        else if (UpdatePlatformInputRegion())
        {
            _cursorTimer.Start();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _cursorTimer.Stop();
        _foldTimer.Stop();
        _motionTimer.Stop();
        _lifetime.Cancel();
        _settingsWindow?.Close();
        if (_screensSubscribed) Screens.Changed -= OnScreensChanged;
        _monitor.SnapshotUpdated -= OnSnapshotUpdated;
        _monitor.CaptureFailed -= OnCaptureFailed;

        _inputRegion.Dispose();
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
            _tooltipTitle.Text = Localization.T("tooltip.systemMonitoring");
            _tooltipValue.Text = Localization.T("tooltip.error");
            System.Diagnostics.Trace.WriteLine(exception);
            _tooltipLine1.Text = Localization.T("tooltip.updateFailed");
            _tooltipLine2.Text = Localization.T("tooltip.retryNext");
            _tooltipLine3.Text = "";
        });
    }

    private void RenderSnapshot(SystemSnapshot snapshot)
    {
        _latestSnapshot = snapshot;
        _settingsWindow?.UpdateDrives(snapshot.Drives);

        var cpu = Math.Clamp(snapshot.CpuPercent, 0, 100);
        var ram = Math.Clamp(snapshot.MemoryUsedPercent, 0, 100);
        var drive = DriveSelection.Resolve(snapshot.Drives, _selectedDrive);

        _cpuRing.SetValue(cpu, $"{cpu:0}%");
        _ramRing.SetValue(ram, $"{ram:0}%");
        _diskRing.SetValue(drive?.UsedPercent, drive is null ? "—" : $"{drive.UsedPercent:0}%");
        _networkRing.SetValue(null, snapshot.Network.Connected ? Localization.T("network.yes") : Localization.T("network.no"));

        if (_hoveredMetric is not null)
        {
            RenderTooltip(_hoveredMetric.Value);
            PositionTooltip(_hoveredMetric.Value);
            UpdatePlatformInputRegion();
        }
    }

    private void PollCursor()
    {
        if (TryGetCursorLocal(out var point))
            UpdatePointer(point);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e) =>
        UpdatePointer(e.GetPosition(this));

    private void UpdatePointer(Point point)
    {
        if (_mode == NotchDisplayMode.Hidden) return;
        if (!_expanded)
        {
            if (HotZoneRect().Contains(point) ||
                (_expansion > 0.05 && ShapeContains(point)))
                Expand();
            return;
        }

        var hovered = MetricIndexAt(point);
        if (hovered is not null)
            SetHoveredMetric(hovered);
        else if (!TooltipLiveRect().Contains(point) && !BridgeRect().Contains(point)
                 && !ShapeContains(point))
            SetHoveredMetric(null);

        if (ShapeContains(point) || HotZoneRect().Contains(point)
            || TooltipLiveRect().Contains(point) || BridgeRect().Contains(point))
            CancelFold();
        else
            ScheduleFold();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (IsInteractive(e.GetPosition(this))) ShowSettings();
            e.Handled = true;
            return;
        }
        if (_mode == NotchDisplayMode.Always) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var point = e.GetPosition(this);
        if (!ShapeContains(point))
            return;

        if (!_expanded)
        {
            Expand();
            return;
        }

        _pinned = !_pinned;
        CancelFold(); // The click is inside the notch; fold only after leaving.
    }

    private void Expand()
    {
        CancelFold();
        if (_expanded) return;
        _expanded = true;
        StartMotion(1);
        UpdatePlatformInputRegion();
    }

    private void ScheduleFold()
    {
        if (_mode == NotchDisplayMode.Hover && !_pinned && _expanded && !_foldTimer.IsEnabled)
            _foldTimer.Start();
    }

    private void CancelFold() => _foldTimer.Stop();

    private void StartMotion(double target)
    {
        _spring.Target = target;
        _motionClock.Restart();
        _motionTimer.Start();
    }

    private void ApplyBackdrop()
    {
        _backdrop = SettingsBackdropSupport.Coerce(_backdrop);
        var glass = Glass.IsGlass(_backdrop);
        var dark = glass ? ThemeIsDark() : true;

        TransparencyLevelHint = _backdrop switch
        {
            SettingsBackdrop.Mica =>
                [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent],
            SettingsBackdrop.Acrylic =>
                [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent],
            _ => [WindowTransparencyLevel.Transparent]
        };

        // Flat deliberately keeps EdgePilot's established dark visual identity regardless of
        // the Settings-window theme. Light/Dark/System affect only the opt-in glass surfaces.
        RequestedThemeVariant = glass
            ? (dark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light)
            : Avalonia.Styling.ThemeVariant.Dark;

        if (glass)
        {
            _notchShape.Stroke = null;
            _notchShape.StrokeThickness = 0;
            _tooltipCard.Background = _backdrop == SettingsBackdrop.Mica
                ? new SolidColorBrush(dark ? Color.FromArgb(0x0D, 255, 255, 255) : Color.FromArgb(0x80, 255, 255, 255))
                : Glass.AcrylicCard(dark);
            _tooltipCard.BorderBrush = _backdrop == SettingsBackdrop.Mica
                ? Glass.MicaCardStroke(dark)
                : Glass.EdgeBrush(dark);
            _tooltipCard.BorderThickness = _backdrop == SettingsBackdrop.Mica
                ? new Thickness(1)
                : Glass.EdgeThickness;
            ApplyTooltipTextColors(dark, lighten: dark && _backdrop == SettingsBackdrop.Acrylic);
        }
        else
        {
            _notchShape.Fill = Brush("#050608");
            _notchShape.Stroke = null;
            _notchShape.StrokeThickness = 0;
            _tooltipCard.Background = Brush("#101318");
            _tooltipCard.BorderBrush = Brush("#2A3039");
            _tooltipCard.BorderThickness = new Thickness(1);
            ApplyTooltipTextColors(dark: true, lighten: false);
        }

        foreach (var ring in new[] { _cpuRing, _ramRing, _diskRing, _networkRing })
            ring.SetTheme(dark);

        UpdateNotchVisual();
    }

    private bool ThemeIsDark() => Glass.ResolveDark(_settingsTheme);

    private void ApplyTooltipTextColors(bool dark, bool lighten)
    {
        if (dark)
        {
            _tooltipTitle.Foreground = TextBrush("#858E9B", lighten, 0.40);
            _tooltipValue.Foreground = Brush("#F5F7FA");
            _tooltipLine1.Foreground = TextBrush("#C9D0D8", lighten, 0.40);
            _tooltipLine2.Foreground = TextBrush("#8B93A1", lighten, 0.45);
            _tooltipLine3.Foreground = TextBrush("#68717E", lighten, 0.50);
        }
        else
        {
            _tooltipTitle.Foreground = Brush("#55585E");
            _tooltipValue.Foreground = Brush("#1A1C21");
            _tooltipLine1.Foreground = Brush("#3A3D42");
            _tooltipLine2.Foreground = Brush("#55585E");
            _tooltipLine3.Foreground = Brush("#6A6D73");
        }
    }

    private static IBrush TextBrush(string hex, bool lighten, double amount) =>
        new SolidColorBrush(lighten ? Glass.Lighten(Color.Parse(hex), amount) : Color.Parse(hex));

    private void UpdateNotchVisual()
    {
        var p = Math.Clamp(_expansion, 0, 1.025);
        var depth = Lerp(CollapsedDepth, ExpandedDepth, p);
        var length = Lerp(CollapsedLength, ExpandedLength, p);
        var geometry = EdgeNotchGeometry.BuildRight(WindowWidth, WindowHeight, depth, length);

        geometry.Transform = new MatrixTransform(NotchLayout.Transform(_edge));
        _notchShape.Data = geometry;
        _notchContent.Clip = geometry;

        if (Glass.IsGlass(_backdrop))
        {
            var dark = ThemeIsDark();
            _notchShape.Fill = _backdrop == SettingsBackdrop.Mica
                ? new SolidColorBrush(dark ? Color.FromArgb(0x0D, 255, 255, 255) : Color.FromArgb(0x40, 255, 255, 255))
                : Glass.AcrylicCard(dark);
        }
        else
        {
            _notchShape.Fill = Brush("#050608");
        }

        var contentProgress = Math.Clamp((p - 0.16) / 0.72, 0, 1);
        _notchContent.Opacity = contentProgress;
        // Cells stay at their final positions; the shared silhouette reveals them.

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

        UpdatePlatformInputRegion();
    }

    private int? MetricIndexAt(Point point)
    {
        point = NotchLayout.ToDesign(point, _edge);
        if (_expansion < 0.82)
            return null;

        var xMin = WindowWidth - ExpandedDepth;
        if (point.X < xMin || point.X > WindowWidth)
            return null;

        var stackTop = (WindowHeight - StackHeight) / 2;
        const double cell = CellHeight;
        const double gap = CellGap;

        for (var index = 0; index < _visibleMetricIndices.Length; index++)
        {
            var top = stackTop + index * (cell + gap);
            if (point.Y >= top && point.Y <= top + cell)
                return _visibleMetricIndices[index];
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
            UpdatePlatformInputRegion();
            return;
        }

        RenderTooltip(index.Value);
        PositionTooltip(index.Value);
        _tooltipCard.IsVisible = true;
        _tooltipCard.Opacity = 1;
        UpdatePlatformInputRegion();
    }

    private void RenderTooltip(int index)
    {
        var snapshot = _latestSnapshot;
        if (snapshot is null)
            return;

        var cpu = Math.Clamp(snapshot.CpuPercent, 0, 100);
        var ram = Math.Clamp(snapshot.MemoryUsedPercent, 0, 100);
        var drive = DriveSelection.Resolve(snapshot.Drives, _selectedDrive);

        switch (index)
        {
            case 0:
                _tooltipTitle.Text = "CPU";
                _tooltipValue.Text = $"{cpu:0}%";
                _tooltipLine1.Text = Localization.T("tooltip.processors", Environment.ProcessorCount);
                _tooltipLine2.Text = snapshot.HostName;
                _tooltipLine3.Text = snapshot.OperatingSystem;
                break;

            case 1:
                _tooltipTitle.Text = Localization.T("tooltip.memory");
                _tooltipValue.Text = $"{ram:0}%";
                _tooltipLine1.Text = Localization.T("bytes.used", DisplayFormat.Bytes(snapshot.MemoryUsedBytes));
                _tooltipLine2.Text = Localization.T("bytes.available", DisplayFormat.Bytes(snapshot.MemoryAvailableBytes));
                _tooltipLine3.Text = Localization.T("bytes.total", DisplayFormat.Bytes(snapshot.MemoryTotalBytes));
                break;

            case 2:
                _tooltipTitle.Text = Localization.T("tooltip.storage");
                if (drive is null)
                {
                    _tooltipValue.Text = "—";
                    _tooltipLine1.Text = _selectedDrive is null ? Localization.T("storage.noDisk") : Localization.T("storage.unavailable");
                    _tooltipLine2.Text = _selectedDrive ?? "";
                    _tooltipLine3.Text = Localization.T("storage.chooseInSettings");
                }
                else
                {
                    _tooltipValue.Text = $"{drive.UsedPercent:0}%";
                    _tooltipLine1.Text = drive.Label;
                    _tooltipLine2.Text = Localization.T("bytes.free", DisplayFormat.Bytes(drive.FreeBytes));
                    _tooltipLine3.Text = Localization.T("bytes.total", DisplayFormat.Bytes(drive.TotalBytes));
                }
                break;

            default:
                _tooltipTitle.Text = Localization.T("tooltip.network");
                _tooltipValue.Text = snapshot.Network.Connected ? Localization.T("network.connected") : Localization.T("network.disconnected");
                _tooltipLine1.Text = snapshot.Network.Connected ? snapshot.Network.InterfaceName : Localization.T("network.noInterface");
                _tooltipLine2.Text = snapshot.Network.Connected
                    ? $"↓ {DisplayFormat.Rate(snapshot.Network.ReceiveBytesPerSecond)}   ↑ {DisplayFormat.Rate(snapshot.Network.SendBytesPerSecond)}"
                    : "";
                _tooltipLine3.Text = snapshot.Network.LinkSpeedBitsPerSecond > 0
                    ? Localization.T("network.linkSpeed", (int)Math.Round(snapshot.Network.LinkSpeedBitsPerSecond / 1_000_000d))
                    : Localization.T("network.uptime", DisplayFormat.Uptime(snapshot.Uptime));
                break;
        }
    }

    public NotchPreferences Preferences => new(_edge, _mode)
    {
        SelectedDrive = _selectedDrive,
        StartAtLogin = _startAtLogin,
        Metrics = _metrics,
        Sensitivity = _sensitivity,
        RefreshIntervalMs = (int)_monitor.RefreshInterval.TotalMilliseconds,
        Language = _language,
        SettingsTheme = _settingsTheme,
        Backdrop = _backdrop
    };

    public void ApplyPreferences(NotchPreferences preferences)
    {
        PreferenceStore.Validate(preferences);
        preferences = PreferenceStore.CoerceForPlatform(preferences);
        CancelFold();
        SetHoveredMetric(null);
        _pinned = false;
        _edge = preferences.Edge;
        _mode = preferences.Mode;
        _selectedDrive = preferences.SelectedDrive;
        _startAtLogin = preferences.StartAtLogin;
        _metrics = preferences.Metrics;
        _sensitivity = preferences.Sensitivity;
        _language = preferences.Language;
        _settingsTheme = preferences.SettingsTheme;
        _backdrop = preferences.Backdrop;
        Localization.SetLanguage(preferences.Language ?? Localization.DetectSystemLanguage());
        _diskRing.SetCaption(Localization.T("ring.disk"));
        _networkRing.SetCaption(Localization.T("ring.network"));
        _monitor.RefreshInterval = TimeSpan.FromMilliseconds(preferences.RefreshIntervalMs);
        _visibleMetricIndices = Enumerable.Range(0, 4).Where(i => ((int)_metrics & (1 << i)) != 0).ToArray();
        _metricStack.Children.Clear();
        MetricRing[] rings = [_cpuRing, _ramRing, _diskRing, _networkRing];
        foreach (var index in _visibleMetricIndices) _metricStack.Children.Add(rings[index]);
        ConfigureLayout();
        ApplyBackdrop();
        _expanded = _mode == NotchDisplayMode.Always;
        StartMotion(_expanded ? 1 : 0);
        UpdateNotchVisual();
        if (_started)
        {
            if (_mode == NotchDisplayMode.Hidden)
            {
                _cursorTimer.Stop();
                Hide();
            }
            else
            {
                Show();
                if (UpdatePlatformInputRegion()) _cursorTimer.Start();
                else _cursorTimer.Stop();
            }
        }
        if (_latestSnapshot is not null) RenderSnapshot(_latestSnapshot);
        Relocate();
        PreferencesChanged?.Invoke();
    }

    public void RequestExit()
    {
        if (ExitRequested is not null) ExitRequested();
        else Close();
    }

    public void ToggleVisibility()
    {
        ApplyPreferences(Preferences with { Mode = _mode == NotchDisplayMode.Hidden ? NotchDisplayMode.Hover : NotchDisplayMode.Hidden });
        if (_mode == NotchDisplayMode.Hidden && !HasTray) ShowSettings();
    }

    public void ShowSettings(string? warning = null)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }
        CancelFold();
        _settingsWindow = new SettingsWindow(Preferences, ApplyPreferences, warning,
            drives: _latestSnapshot?.Drives, savePreferences: SavePreferences,
            exit: RequestExit, hasTray: HasTray, reopen: () => Dispatcher.UIThread.Post(() => ShowSettings()));
        _settingsWindow.Closed += (_, _) =>
        {
            _settingsWindow = null;
            if (_mode == NotchDisplayMode.Hidden && !HasTray && !_lifetime.IsCancellationRequested)
                Close();
        };
        _settingsWindow.Show();
    }

    private void ConfigureLayout()
    {
        var size = NotchLayout.WindowSize(_edge);
        Width = size.Width;
        Height = size.Height;
        var root = (Canvas)Content!;
        root.Width = _notchContent.Width = size.Width;
        root.Height = _notchContent.Height = size.Height;
        var horizontal = NotchLayout.Horizontal(_edge);
        _metricStack.Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
        _metricStack.Width = horizontal ? StackHeight : ExpandedDepth;
        _metricStack.Height = horizontal ? ExpandedDepth : StackHeight;
        foreach (var ring in _metricStack.Children)
        {
            ring.Width = horizontal ? CellHeight : 62;
            ring.VerticalAlignment = VerticalAlignment.Center;
        }
        var bounds = NotchLayout.ToScreen(new Rect(WindowWidth - ExpandedDepth,
            (WindowHeight - StackHeight) / 2, ExpandedDepth, StackHeight), _edge);
        Canvas.SetLeft(_metricStack, bounds.X);
        Canvas.SetTop(_metricStack, bounds.Y);
    }

    private void PositionTooltip(int index)
    {
        const double tooltipWidth = 270;
        const double gap = 16;
        // Measure while visible; invisible controls otherwise report zero desired size.
        _tooltipCard.IsVisible = true;
        _tooltipCard.Measure(new Size(tooltipWidth, double.PositiveInfinity));
        var height = Math.Max(174, _tooltipCard.DesiredSize.Height);
        var center = NotchLayout.ToScreen(new Point(WindowWidth - ExpandedDepth / 2,
            (WindowHeight - StackHeight) / 2 + Array.IndexOf(_visibleMetricIndices, index) * (CellHeight + CellGap) + CellHeight / 2), _edge);
        var x = _edge switch
        {
            EdgeSide.Right => Width - ExpandedDepth - gap - tooltipWidth,
            EdgeSide.Left => ExpandedDepth + gap,
            _ => Math.Clamp(center.X - tooltipWidth / 2, 16, Width - tooltipWidth - 16)
        };
        var y = _edge switch
        {
            EdgeSide.Top => ExpandedDepth + gap,
            EdgeSide.Bottom => Height - ExpandedDepth - gap - height,
            _ => Math.Clamp(center.Y - height / 2, 16, Math.Max(16, Height - height - 16))
        };
        Canvas.SetLeft(_tooltipCard, x);
        Canvas.SetTop(_tooltipCard, Math.Max(0, y));
    }

    private Rect HotZoneRect() => NotchLayout.ToScreen(new Rect(
        WindowWidth - HotZoneDepth, (WindowHeight - HotZoneLength) / 2,
        HotZoneDepth, HotZoneLength), _edge);

    private Rect ShapeRect()
    {
        var p = Math.Clamp(_expansion, 0, 1.025);
        var depth = Lerp(CollapsedDepth, ExpandedDepth, p);
        var length = Lerp(CollapsedLength, ExpandedLength, p);
        return NotchLayout.ToScreen(new Rect(WindowWidth - depth, (WindowHeight - length) / 2, depth, length), _edge);
    }

    private Rect[] ShapeInputRects()
    {
        var p = Math.Clamp(_expansion, 0, 1.025);
        var depth = Lerp(CollapsedDepth, ExpandedDepth, p);
        var length = Lerp(CollapsedLength, ExpandedLength, p);
        return EdgeNotchGeometry.BuildInputStripsRight(WindowWidth, WindowHeight, depth, length)
            .Select(rect => NotchLayout.ToScreen(rect, _edge))
            .ToArray();
    }

    private bool ShapeContains(Point point) => ShapeInputRects().Any(rect => rect.Contains(point));

    private Rect TooltipLiveRect()
    {
        if (!_tooltipCard.IsVisible || _hoveredMetric is null)
            return default;

        var x = Canvas.GetLeft(_tooltipCard);
        var y = Canvas.GetTop(_tooltipCard);
        return new Rect(x, y, _tooltipCard.Width, Math.Max(_tooltipCard.Bounds.Height, 174));
    }

    private Rect BridgeRect()
    {
        if (_hoveredMetric is null || !_tooltipCard.IsVisible)
            return default;

        var tip = TooltipLiveRect();
        var shape = ShapeRect();
        return _edge switch
        {
            EdgeSide.Left => new Rect(shape.Right, tip.Y, Math.Max(0, tip.Left - shape.Right), tip.Height),
            EdgeSide.Top => new Rect(tip.X, shape.Bottom, tip.Width, Math.Max(0, tip.Top - shape.Bottom)),
            EdgeSide.Bottom => new Rect(tip.X, tip.Bottom, tip.Width, Math.Max(0, shape.Top - tip.Bottom)),
            _ => new Rect(tip.Right, tip.Y, Math.Max(0, shape.Left - tip.Right), tip.Height)
        };
    }

    private Rect[] InteractiveRects()
    {
        if (_mode == NotchDisplayMode.Hidden)
            return [];

        var shape = ShapeInputRects();
        var regions = new List<Rect>(shape.Length + 3);
        if (_mode == NotchDisplayMode.Hover)
            regions.Add(HotZoneRect());

        regions.AddRange(shape);

        var tooltip = TooltipLiveRect();
        if (tooltip.Width > 0 && tooltip.Height > 0)
            regions.Add(tooltip);

        var bridge = BridgeRect();
        if (bridge.Width > 0 && bridge.Height > 0)
            regions.Add(bridge);

        return regions.ToArray();
    }

    private Rect[] GlassVisibleRects()
    {
        if (_mode == NotchDisplayMode.Hidden)
            return [];

        var shape = ShapeInputRects();
        var regions = new List<Rect>(shape.Length + 180);
        regions.AddRange(shape);

        var tooltip = TooltipLiveRect();
        if (tooltip.Width > 0 && tooltip.Height > 0)
            regions.AddRange(RoundedRectStrips(tooltip, 14));

        return regions.ToArray();
    }

    private static Rect[] RoundedRectStrips(Rect rect, double radius)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return [];

        radius = Math.Clamp(radius, 0, Math.Min(rect.Width, rect.Height) / 2);
        if (radius < 0.5)
            return [rect];

        var strips = new List<Rect>((int)Math.Ceiling(rect.Height));
        for (var y = rect.Top; y < rect.Bottom; y += 1)
        {
            var height = Math.Min(1, rect.Bottom - y);
            var centerY = y + height / 2;
            var inset = 0d;

            if (centerY < rect.Top + radius)
            {
                var dy = rect.Top + radius - centerY;
                inset = radius - Math.Sqrt(Math.Max(0, radius * radius - dy * dy));
            }
            else if (centerY > rect.Bottom - radius)
            {
                var dy = centerY - (rect.Bottom - radius);
                inset = radius - Math.Sqrt(Math.Max(0, radius * radius - dy * dy));
            }

            var width = rect.Width - inset * 2;
            if (width > 0)
                strips.Add(new Rect(rect.Left + inset, y, width, height));
        }

        return strips.ToArray();
    }

    private bool IsInteractive(Point point) => InteractiveRects().Any(rect => rect.Contains(point));

    private bool UpdatePlatformInputRegion()
    {
        if ((!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux()) || !_started || !IsVisible)
            return true;

        // On Windows SetWindowRgn is both the visual bounding region and the hit-test region.
        // A glass backdrop would therefore become visible inside Hover's invisible hot-zone or
        // tooltip bridge if we passed the normal interaction set. Windows already polls the
        // global cursor, so glass can safely use only the visible pill + rounded popup while
        // preserving Hover/bridge behavior. Linux remains Flat and keeps its ShapeInput hot-zone.
        var nativeRects = OperatingSystem.IsWindows() && Glass.IsGlass(_backdrop)
            ? GlassVisibleRects()
            : InteractiveRects();

        if (_inputRegion.TryApply(nativeRects, RenderScaling, new Size(Width, Height)))
            return true;

        // Failing closed is intentional: a hidden edge surface is preferable to leaving a
        // transparent topmost rectangle that blocks the user's desktop or another application.
        System.Diagnostics.Trace.WriteLine("EdgePilot disabled the edge surface because a safe native input region could not be established.");
        _cursorTimer.Stop();
        Hide();
        return false;
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