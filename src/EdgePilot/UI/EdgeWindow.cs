using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using EdgePilot.Core;
using EdgePilot.Core.Monitoring;
using EdgePilot.Platform;
using Path = Avalonia.Controls.Shapes.Path;

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

    private readonly Win32Properties.CustomWndProcHookCallback? _wndProcHook;

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
        if (_started) return;
        _started = true;
        Screens.Changed += OnScreensChanged;
        _screensSubscribed = true;
        _cursorTimer.Start();
        _ = Task.Run(() => _monitor.RunAsync(_lifetime.Token));
        if (_mode == NotchDisplayMode.Hidden) { _cursorTimer.Stop(); Hide(); }
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

        if (_wndProcHook is not null)
            Win32Properties.RemoveWndProcHookCallback(this, _wndProcHook);



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
                (_expansion > 0.05 && ShapeRect().Contains(point)))
                Expand();
            return;
        }

        var hovered = MetricIndexAt(point);
        if (hovered is not null)
            SetHoveredMetric(hovered);
        else if (!TooltipLiveRect().Contains(point) && !BridgeRect().Contains(point)
                 && !ExpandedLiveRect().Contains(point))
            SetHoveredMetric(null);

        if (ExpandedLiveRect().Contains(point) || HotZoneRect().Contains(point)
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
        if (!ShapeRect().Contains(point))
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
        var glass = Glass.IsGlass(_backdrop);

        // Mica and Acrylic are real OS backdrops; UpdateNotchVisual clips the window to the
        // pill (and popup) so the backdrop only shows there. Flat draws its own opaque tab
        // over a per-pixel-transparent window.
        var mica = _backdrop == SettingsBackdrop.Mica;
        TransparencyLevelHint = _backdrop switch
        {
            SettingsBackdrop.Mica =>
                [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Transparent],
            SettingsBackdrop.Acrylic =>
                [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent],
            _ => [WindowTransparencyLevel.Transparent]
        };

        // The notch and popup track the settings theme, so light gets dark text and vice
        // versa — the same flip the settings window does.
        var dark = ThemeIsDark();

        // Mica is a theme-toned OS base, so the window must render in that theme for the
        // wallpaper tint to come out light in Light mode; Flat/Acrylic keep the dark notch.
        RequestedThemeVariant = mica && !dark
            ? Avalonia.Styling.ThemeVariant.Light
            : Avalonia.Styling.ThemeVariant.Dark;

        if (glass)
        {
            // Pill fill is set per-frame in UpdateNotchVisual (collapsed reads stronger).
            _notchShape.Stroke = null;
            _notchShape.StrokeThickness = 0;

            // Mica lays only a thin layer over the OS wallpaper base so it doesn't read as a
            // solid tab; the light layer is a touch denser so the popup's dark text still reads.
            _tooltipCard.Background = mica
                ? new SolidColorBrush(dark ? Color.FromArgb(0x0D, 255, 255, 255) : Color.FromArgb(0x80, 255, 255, 255))
                : Glass.AcrylicCard(dark);
            _tooltipCard.BorderBrush = mica ? Glass.MicaCardStroke(dark) : Glass.EdgeBrush(dark);
            _tooltipCard.BorderThickness = mica ? new Thickness(1) : Glass.EdgeThickness;
            ApplyTooltipTextColors(dark, lighten: dark && !mica);
        }
        else
        {
            // Flat is an opaque tab: dark keeps the owner's near-black; light is a clean solid
            // panel (deliberately not the wallpaper-tinted look a blurred surface would give).
            _notchShape.Fill = Brush(dark ? "#050608" : "#EDEEF1");
            _notchShape.Stroke = null;
            _notchShape.StrokeThickness = 0;

            _tooltipCard.Background = Brush(dark ? "#101318" : "#FBFBFC");
            _tooltipCard.BorderBrush = Brush(dark ? "#2A3039" : "#D4D7DC");
            _tooltipCard.BorderThickness = new Thickness(1);
            ApplyTooltipTextColors(dark, lighten: false);
        }

        foreach (var ring in new[] { _cpuRing, _ramRing, _diskRing, _networkRing })
            ring.SetTheme(dark);

        UpdateNotchVisual();
    }

    // The notch is a shaped window with its own bespoke painting, but it resolves light/dark
    // from the same shared helper the full-window screens use.
    private bool ThemeIsDark() => Glass.ResolveDark(_settingsTheme);

    // Light theme uses dark popup text; dark theme keeps the light greys (lifted over Acrylic).
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
            // Collapsed and expanded share the same surface layer over the OS backdrop.
            var dark = ThemeIsDark();
            _notchShape.Fill = _backdrop == SettingsBackdrop.Mica
                // Thin layer over the OS Mica wallpaper base so the pill shows the tint, not a solid.
                ? new SolidColorBrush(dark ? Color.FromArgb(0x0D, 255, 255, 255) : Color.FromArgb(0x40, 255, 255, 255))
                : Glass.AcrylicCard(dark);
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

        UpdateRegion();
    }

    // Clip the window to the pill silhouette (plus the popup when shown) so the OS acrylic
    // shows only there. Cleared in flat mode, where per-pixel transparency shapes it instead.
    private void UpdateRegion()
    {
        if (!Glass.IsGlass(_backdrop))
        {
            NotchRegion.Apply(this, null, null, 0);
            return;
        }

        var scale = RenderScaling <= 0 ? 1 : RenderScaling;
        // Rebuild the outline in design space, apply the same edge transform, then scale.
        var p = Math.Clamp(_expansion, 0, 1.025);
        var depth = Lerp(CollapsedDepth, ExpandedDepth, p);
        var length = Lerp(CollapsedLength, ExpandedLength, p);
        var matrix = NotchLayout.Transform(_edge);
        var outline = EdgeNotchGeometry.OutlinePoints(WindowWidth, WindowHeight, depth, length);
        var pill = new Point[outline.Length];
        for (var i = 0; i < outline.Length; i++)
        {
            var q = outline[i].Transform(matrix);
            pill[i] = new Point(q.X * scale, q.Y * scale);
        }

        Rect? tooltip = null;
        if (_tooltipCard.IsVisible && _hoveredMetric is not null)
        {
            var x = Canvas.GetLeft(_tooltipCard);
            var y = Canvas.GetTop(_tooltipCard);
            var h = Math.Max(174, _tooltipCard.Bounds.Height);
            tooltip = new Rect(x * scale, y * scale, 270 * scale, h * scale);
        }

        NotchRegion.Apply(this, pill, tooltip, 14 * scale);
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
            if (Glass.IsGlass(_backdrop)) UpdateRegion();
            return;
        }

        RenderTooltip(index.Value);
        PositionTooltip(index.Value);
        _tooltipCard.IsVisible = true;
        _tooltipCard.Opacity = 1;
        if (Glass.IsGlass(_backdrop)) UpdateRegion();
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
        Backdrop = _backdrop,
        SettingsTheme = _settingsTheme
    };

    public void ApplyPreferences(NotchPreferences preferences)
    {
        PreferenceStore.Validate(preferences);
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
        _backdrop = preferences.Backdrop;
        _settingsTheme = preferences.SettingsTheme;
        ApplyBackdrop();
        Localization.SetLanguage(preferences.Language ?? Localization.DetectSystemLanguage());
        _diskRing.SetCaption(Localization.T("ring.disk"));
        _networkRing.SetCaption(Localization.T("ring.network"));
        _monitor.RefreshInterval = TimeSpan.FromMilliseconds(preferences.RefreshIntervalMs);
        _visibleMetricIndices = Enumerable.Range(0, 4).Where(i => ((int)_metrics & (1 << i)) != 0).ToArray();
        _metricStack.Children.Clear();
        MetricRing[] rings = [_cpuRing, _ramRing, _diskRing, _networkRing];
        foreach (var index in _visibleMetricIndices) _metricStack.Children.Add(rings[index]);
        ConfigureLayout();
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
                _cursorTimer.Start();
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

        if (Glass.IsGlass(_backdrop)) UpdateRegion();
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

    private Rect ExpandedLiveRect() => ShapeRect();

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

    private bool IsInteractive(Point point)
    {
        if (_mode == NotchDisplayMode.Hidden) return false;
        if (!_expanded)
            return HotZoneRect().Contains(point) || ShapeRect().Contains(point);

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
