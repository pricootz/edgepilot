using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using EdgePilot.Core;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow : GlassWindow
{
    private sealed record LanguageChoice(Language? Value, string Caption)
    {
        public override string ToString() => Caption;
    }

    private static int _pendingInitialPage = -1;
    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#FF8A3D"));
    private static readonly IBrush AccentSoftBrush = new SolidColorBrush(Color.FromArgb(38, 255, 138, 61));
    private static readonly IBrush PreviewBackgroundBrush = new SolidColorBrush(Color.Parse("#101114"));
    private static readonly IBrush PreviewBorderBrush = new SolidColorBrush(Color.Parse("#404247"));
    private static readonly SolidColorBrush MutedBrush = new(Color.Parse("#8D9096"));
    private static readonly VisibleMetrics[] MetricOptions =
        { VisibleMetrics.Cpu, VisibleMetrics.Memory, VisibleMetrics.Disk, VisibleMetrics.Network };

    private readonly ComboBox _drive;
    private readonly ComboBox _refresh;
    private readonly ComboBox _language;
    private readonly CheckBox[] _metrics;
    private readonly CheckBox _autostart;
    private readonly Button _applyButton;
    private readonly Button _resetButton;
    private readonly TextBlock _status;
    private readonly ContentControl _pageHost;
    private readonly Border _header;
    private readonly Border _sidebar;
    private readonly Border _footer;
    private readonly Border _previewFrame;
    private readonly Border _previewNotch;
    private readonly Button[] _edgeButtons;
    private readonly Button[] _modeButtons;
    private readonly Button[] _sensitivityButtons;
    private readonly Button[] _themeButtons;
    private readonly Button[] _backdropButtons;
    private readonly Dictionary<SettingsPage, Button> _navButtons = new();
    private readonly Dictionary<SettingsPage, Control> _pages = new();
    private readonly List<Border> _cards = new();
    private readonly List<StackPanel> _pageStacks = new();
    private readonly string? _initialDrive;
    private readonly int[] _intervals = { 500, 1000, 2000, 5000 };

    private Border _diskCard = null!;
    private Border _surfaceCard = null!;
    private Border[] _metricTiles = Array.Empty<Border>();
    private Grid _body = null!;
    private Grid _previewLayout = null!;
    private StackPanel _positionOptions = null!;
    private Grid _metricGrid = null!;
    private Border _versionBadge = null!;
    private Button _exitButton = null!;

    private NotchPreferences _savedPreferences;
    private EdgeSide _selectedEdge;
    private NotchDisplayMode _selectedMode;
    private HoverSensitivity _selectedSensitivity;
    private SettingsThemePreference _selectedTheme;
    private SettingsBackdrop _selectedBackdrop;
    private SettingsPage _selectedPage;
    private bool _ready;
    private bool _darkTheme;

    public SettingsWindow(NotchPreferences current, Action<NotchPreferences> apply, string? warning = null,
        string? storagePath = null, IReadOnlyList<DriveSnapshot>? drives = null,
        Action<NotchPreferences>? savePreferences = null, Action? exit = null, bool hasTray = false,
        Action? reopen = null, int initialPage = -1)
    {
        _savedPreferences = current;
        _selectedEdge = current.Edge;
        _selectedMode = current.Mode;
        _selectedSensitivity = current.Sensitivity;
        _selectedTheme = current.SettingsTheme;
        _selectedBackdrop = current.Backdrop;
        _initialDrive = current.SelectedDrive;

        Title = Localization.T("settings.title");
        Icon = AppIcon.Load();
        Width = 920;
        Height = 680;
        MinWidth = 680;
        MinHeight = 520;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = ThemeVariantFor(current.SettingsTheme);

        _drive = new ComboBox
        {
            Foreground = PrimaryBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxDropDownHeight = 280,
            MaxWidth = 460
        };
        UpdateDrives(drives ?? Array.Empty<DriveSnapshot>());

        _refresh = new ComboBox
        {
            ItemsSource = new[]
            {
                Localization.T("refresh.0_5s"), Localization.T("refresh.1s"),
                Localization.T("refresh.2s"), Localization.T("refresh.5s")
            },
            SelectedIndex = Array.IndexOf(_intervals, current.RefreshIntervalMs),
            Foreground = PrimaryBrush,
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = 320,
            MinWidth = 210
        };

        var languageChoices = new List<LanguageChoice>
        {
            new(null, Localization.T("language.autoNamed", Localization.SystemLanguageName))
        };
        languageChoices.AddRange(Localization.Available.Select(option => new LanguageChoice(option.Value, option.Name)));
        var selectedLanguage = current.Language is { } explicitLanguage
            ? Localization.NormalizePreference(explicitLanguage)
            : (Language?)null;
        var languageIndex = languageChoices.FindIndex(choice => choice.Value == selectedLanguage);
        _language = new ComboBox
        {
            ItemsSource = languageChoices,
            SelectedIndex = languageIndex >= 0 ? languageIndex : 0,
            Foreground = PrimaryBrush,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 210,
            MaxWidth = 320
        };

        var metricNames = new[]
        {
            Localization.T("metric.cpu"), Localization.T("metric.memory"),
            Localization.T("metric.disk"), Localization.T("metric.network")
        };
        _metrics = MetricOptions.Select((flag, index) => new CheckBox
        {
            Content = metricNames[index],
            IsChecked = current.Metrics.HasFlag(flag),
            Foreground = PrimaryBrush,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        }).ToArray();

        _autostart = new CheckBox
        {
            Content = Localization.T("startup.autostartLabel"),
            IsChecked = current.StartAtLogin,
            Foreground = PrimaryBrush,
            FontWeight = FontWeight.SemiBold
        };

        _previewNotch = new Border
        {
            Background = AccentBrush,
            CornerRadius = new CornerRadius(6)
        };
        var previewGrid = new Grid();
        previewGrid.Children.Add(new TextBlock
        {
            Text = "DESKTOP",
            Foreground = MutedBrush,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        previewGrid.Children.Add(_previewNotch);
        _previewFrame = new Border
        {
            Width = 292,
            Height = 178,
            Background = PreviewBackgroundBrush,
            BorderBrush = PreviewBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Child = previewGrid,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _edgeButtons =
        [
            SegmentButton("▸", Localization.T("edge.right"), () => SelectEdge(EdgeSide.Right)),
            SegmentButton("◂", Localization.T("edge.left"), () => SelectEdge(EdgeSide.Left)),
            SegmentButton("▴", Localization.T("edge.top"), () => SelectEdge(EdgeSide.Top)),
            SegmentButton("▾", Localization.T("edge.bottom"), () => SelectEdge(EdgeSide.Bottom))
        ];

        _modeButtons =
        [
            SegmentButton("◌", Localization.T("mode.hover"), () => SelectMode(NotchDisplayMode.Hover)),
            SegmentButton("●", Localization.T("mode.always"), () => SelectMode(NotchDisplayMode.Always)),
            SegmentButton("○", Localization.T("mode.hidden"), () => SelectMode(NotchDisplayMode.Hidden))
        ];

        _sensitivityButtons =
        [
            SegmentButton(null, Localization.T("sensitivity.precise"), () => SelectSensitivity(HoverSensitivity.Precise)),
            SegmentButton(null, Localization.T("sensitivity.normal"), () => SelectSensitivity(HoverSensitivity.Normal)),
            SegmentButton(null, Localization.T("sensitivity.wide"), () => SelectSensitivity(HoverSensitivity.Wide))
        ];

        _themeButtons =
        [
            SegmentButton("◐", Localization.T("theme.system"), () => SelectTheme(SettingsThemePreference.System)),
            SegmentButton("☀", Localization.T("theme.light"), () => SelectTheme(SettingsThemePreference.Light)),
            SegmentButton("●", Localization.T("theme.dark"), () => SelectTheme(SettingsThemePreference.Dark))
        ];

        _backdropButtons =
        [
            SegmentButton(null, Localization.T("surface.flat"), () => SelectBackdrop(SettingsBackdrop.Flat)),
            SegmentButton(null, Localization.T("surface.mica"), () => SelectBackdrop(SettingsBackdrop.Mica)),
            SegmentButton(null, Localization.T("surface.acrylic"), () => SelectBackdrop(SettingsBackdrop.Acrylic))
        ];

        _status = new TextBlock
        {
            Text = warning ?? Localization.T("settings.status.saved"),
            Foreground = PrimaryBrush,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            MaxLines = 2
        };

        _applyButton = new Button
        {
            Content = ButtonContent("✓", Localization.T("settings.saveChanges")),
            Foreground = Brushes.White,
            Background = AccentBrush,
            BorderBrush = AccentBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(17, 9),
            MinWidth = 146,
            IsEnabled = false
        };
        _resetButton = new Button
        {
            Content = ButtonContent("↶", Localization.T("settings.resetChanges")),
            Foreground = PrimaryBrush,
            Background = Brushes.Transparent,
            BorderBrush = ControlBorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 9),
            MinWidth = 100,
            IsEnabled = false
        };
        RegisterActionButton(_applyButton, ActionButtonRole.Primary);
        RegisterActionButton(_resetButton, ActionButtonRole.Secondary);

        _drive.SelectionChanged += (_, _) => MarkDirty();
        _refresh.SelectionChanged += (_, _) => MarkDirty();
        _language.SelectionChanged += (_, _) => MarkDirty();
        foreach (var metric in _metrics) metric.Click += (_, _) => OnMetricChanged();
        _autostart.Click += (_, _) => MarkDirty();

        _resetButton.Click += (_, _) => ResetToSaved();
        _applyButton.Click += (_, _) => SaveChanges(apply, savePreferences, storagePath, hasTray, reopen);

        _pageHost = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };

        _pages[SettingsPage.General] = BuildGeneralPage();
        _pages[SettingsPage.Edge] = BuildEdgePage();
        _pages[SettingsPage.Monitor] = BuildMonitorPage();
        _pages[SettingsPage.Behavior] = BuildBehaviorPage();
        _pages[SettingsPage.Startup] = BuildStartupPage(exit);
        _pages[SettingsPage.About] = BuildAboutPage();

        _header = BuildHeader();
        _sidebar = BuildSidebar();
        _footer = BuildFooter();

        var shell = new Grid
        {
            RowDefinitions = new RowDefinitions("76,*,68")
        };
        shell.Children.Add(_header);
        Grid.SetRow(_header, 0);

        _body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("184,*")
        };
        _body.Children.Add(_sidebar);
        Grid.SetColumn(_sidebar, 0);
        _body.Children.Add(_pageHost);
        Grid.SetColumn(_pageHost, 1);
        shell.Children.Add(_body);
        Grid.SetRow(_body, 1);

        shell.Children.Add(_footer);
        Grid.SetRow(_footer, 2);
        Content = shell;

        _ready = true;
        SelectEdge(current.Edge, markDirty: false);
        SelectMode(current.Mode, markDirty: false);
        SelectSensitivity(current.Sensitivity, markDirty: false);
        SelectTheme(current.SettingsTheme, markDirty: false);
        SelectBackdrop(current.Backdrop, markDirty: false);
        UpdateConditionalSettings();
        var pendingPage = initialPage >= 0 ? initialPage : Interlocked.Exchange(ref _pendingInitialPage, -1);
        var firstPage = Enum.IsDefined(typeof(SettingsPage), pendingPage) ? (SettingsPage)pendingPage : SettingsPage.General;
        ShowPage(firstPage);
        ApplyTheme();
        ApplyResponsiveLayout(Width);

        ActualThemeVariantChanged += (_, _) => ApplyTheme();
        SizeChanged += (_, e) => ApplyResponsiveLayout(e.NewSize.Width);
    }

    public void UpdateDrives(IReadOnlyList<DriveSnapshot> drives)
    {
        var selected = _drive?.SelectedItem is DriveChoice current ? current.Name : _initialDrive;
        var choices = DriveSelection.Choices(drives, selected);
        if (_drive?.ItemsSource is IReadOnlyList<DriveChoice> old && old.SequenceEqual(choices)) return;
        if (_drive is null) return;
        _drive.ItemsSource = choices;
        _drive.SelectedItem = choices.First(x => DriveSelection.PathComparer.Equals(x.Name, selected));
    }

    private void ShowPage(SettingsPage page)
    {
        _selectedPage = page;
        _pageHost.Content = _pages[page];
        UpdateNavigationIconStates(page);
    }

    private void SelectEdge(EdgeSide edge, bool markDirty = true)
    {
        _selectedEdge = edge;
        UpdateSegments(_edgeButtons, (int)edge);
        UpdatePreview();
        if (markDirty) MarkDirty();
    }

    private void SelectMode(NotchDisplayMode mode, bool markDirty = true)
    {
        _selectedMode = mode;
        UpdateSegments(_modeButtons, (int)mode);
        if (markDirty) MarkDirty();
    }

    private void SelectSensitivity(HoverSensitivity sensitivity, bool markDirty = true)
    {
        _selectedSensitivity = sensitivity;
        UpdateSegments(_sensitivityButtons, (int)sensitivity);
        if (markDirty) MarkDirty();
    }

    private void SelectTheme(SettingsThemePreference theme, bool markDirty = true)
    {
        _selectedTheme = theme;
        UpdateSegments(_themeButtons, (int)theme);
        RequestedThemeVariant = ThemeVariantFor(theme);
        ApplyTheme();
        if (markDirty) MarkDirty();
    }

    private void SelectBackdrop(SettingsBackdrop backdrop, bool markDirty = true)
    {
        _selectedBackdrop = backdrop;
        UpdateSegments(_backdropButtons, (int)backdrop);
        ApplyTheme();
        if (markDirty) MarkDirty();
    }

    private static ThemeVariant ThemeVariantFor(SettingsThemePreference theme) => theme switch
    {
        SettingsThemePreference.Light => ThemeVariant.Light,
        SettingsThemePreference.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };

    private void UpdateSegments(IReadOnlyList<Button> buttons, int selectedIndex) =>
        SetSegmentSelection(buttons, selectedIndex);

    private void UpdatePreview()
    {
        _previewNotch.HorizontalAlignment = _selectedEdge switch
        {
            EdgeSide.Left => HorizontalAlignment.Left,
            EdgeSide.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center
        };
        _previewNotch.VerticalAlignment = _selectedEdge switch
        {
            EdgeSide.Top => VerticalAlignment.Top,
            EdgeSide.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center
        };
        var horizontal = _selectedEdge is EdgeSide.Top or EdgeSide.Bottom;
        _previewNotch.Width = horizontal ? 64 : 12;
        _previewNotch.Height = horizontal ? 12 : 64;
        _previewNotch.CornerRadius = new CornerRadius(6);
    }

    private void OnMetricChanged()
    {
        UpdateConditionalSettings();
        UpdateMetricTileStates();
        MarkDirty();
    }

    private void UpdateConditionalSettings()
    {
        if (_diskCard is not null) _diskCard.IsVisible = _metrics[2].IsChecked == true;
    }

    private void UpdateMetricTileStates() => PaintMetricTiles();

    private VisibleMetrics SelectedMetrics()
    {
        VisibleMetrics selected = 0;
        for (var i = 0; i < _metrics.Length; i++)
            if (_metrics[i].IsChecked == true) selected |= MetricOptions[i];
        return selected;
    }

    private NotchPreferences CurrentPreferences()
    {
        return new NotchPreferences(_selectedEdge, _selectedMode)
        {
            RefreshIntervalMs = _intervals[_refresh.SelectedIndex],
            Sensitivity = _selectedSensitivity,
            Metrics = SelectedMetrics(),
            SelectedDrive = (_drive.SelectedItem as DriveChoice)?.Name,
            StartAtLogin = _autostart.IsChecked == true,
            Language = (_language.SelectedItem as LanguageChoice)?.Value,
            SettingsTheme = _selectedTheme,
            Backdrop = _selectedBackdrop
        };
    }

    private void MarkDirty()
    {
        if (!_ready) return;
        var current = CurrentPreferences();
        var dirty = current != _savedPreferences;
        _applyButton.IsEnabled = dirty;
        _resetButton.IsEnabled = dirty;
        _status.Text = dirty ? Localization.T("settings.status.unsaved") : Localization.T("settings.status.saved");
        RefreshActionButtonVisuals();
    }

    private void ResetToSaved()
    {
        _ready = false;
        _selectedEdge = _savedPreferences.Edge;
        _selectedMode = _savedPreferences.Mode;
        _selectedSensitivity = _savedPreferences.Sensitivity;
        _selectedTheme = _savedPreferences.SettingsTheme;
        _selectedBackdrop = _savedPreferences.Backdrop;
        _refresh.SelectedIndex = Array.IndexOf(_intervals, _savedPreferences.RefreshIntervalMs);
        for (var i = 0; i < _metrics.Length; i++)
            _metrics[i].IsChecked = _savedPreferences.Metrics.HasFlag(MetricOptions[i]);
        _autostart.IsChecked = _savedPreferences.StartAtLogin;

        if (_language.ItemsSource is IReadOnlyList<LanguageChoice> languages)
        {
            var selected = _savedPreferences.Language is { } explicitLanguage
                ? Localization.NormalizePreference(explicitLanguage)
                : (Language?)null;
            _language.SelectedItem = languages.First(choice => choice.Value == selected);
        }
        if (_drive.ItemsSource is IReadOnlyList<DriveChoice> choices)
            _drive.SelectedItem = choices.First(x => DriveSelection.PathComparer.Equals(x.Name, _savedPreferences.SelectedDrive));

        UpdateSegments(_edgeButtons, (int)_selectedEdge);
        UpdateSegments(_modeButtons, (int)_selectedMode);
        UpdateSegments(_sensitivityButtons, (int)_selectedSensitivity);
        UpdateSegments(_themeButtons, (int)_selectedTheme);
        UpdateSegments(_backdropButtons, (int)_selectedBackdrop);
        RequestedThemeVariant = ThemeVariantFor(_selectedTheme);
        ApplyTheme();
        UpdatePreview();
        UpdateConditionalSettings();
        UpdateMetricTileStates();
        _ready = true;
        _applyButton.IsEnabled = false;
        _resetButton.IsEnabled = false;
        _status.Text = Localization.T("settings.status.reset");
        RefreshActionButtonVisuals();
    }

    private void SaveChanges(Action<NotchPreferences> apply, Action<NotchPreferences>? savePreferences,
        string? storagePath, bool hasTray, Action? reopen)
    {
        var selectedMetrics = SelectedMetrics();
        if (selectedMetrics == 0)
        {
            _status.Text = Localization.T("settings.selectMetric");
            ShowPage(SettingsPage.Monitor);
            return;
        }

        var value = CurrentPreferences();
        var languageChanged = value.Language != _savedPreferences.Language;
        try
        {
            if (savePreferences is not null) savePreferences(value);
            else PreferenceStore.Save(storagePath ?? PreferenceStore.DefaultPath, value);
            apply(value);
            _savedPreferences = value;
            _applyButton.IsEnabled = false;
            _resetButton.IsEnabled = false;
            RefreshActionButtonVisuals();

            if (languageChanged && reopen is not null)
            {
                Interlocked.Exchange(ref _pendingInitialPage, (int)_selectedPage);
                reopen();
                Close();
                return;
            }

            _status.Text = value.Mode == NotchDisplayMode.Hidden
                ? (hasTray ? Localization.T("settings.hiddenWithTray") : Localization.T("settings.hiddenNoTray"))
                : Localization.T("settings.saved");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            _status.Text = Localization.T("settings.saveFailed");
            _applyButton.IsEnabled = true;
            RefreshActionButtonVisuals();
        }
    }

    private void ApplyResponsiveLayout(double width)
    {
        if (_body is null) return;
        var compact = width < 800;

        _body.ColumnDefinitions = new ColumnDefinitions(compact ? "154,*" : "184,*");
        _versionBadge.IsVisible = width >= 760;

        foreach (var stack in _pageStacks)
            stack.Margin = compact ? new Thickness(18, 20, 18, 24) : new Thickness(28, 24, 28, 28);

        _previewLayout.ColumnDefinitions = compact ? new ColumnDefinitions("*") : new ColumnDefinitions("Auto,*");
        _previewLayout.RowDefinitions = compact ? new RowDefinitions("Auto,Auto") : new RowDefinitions("Auto");
        Grid.SetColumn(_positionOptions, compact ? 0 : 1);
        Grid.SetRow(_positionOptions, compact ? 1 : 0);
        _positionOptions.Margin = compact ? new Thickness(0, 16, 0, 0) : new Thickness(0);
        _previewFrame.Width = compact ? 260 : 292;
        _previewFrame.Height = compact ? 158 : 178;

        _metricGrid.ColumnDefinitions = compact ? new ColumnDefinitions("*") : new ColumnDefinitions("*,*");
        _metricGrid.RowDefinitions = compact
            ? new RowDefinitions("Auto,Auto,Auto,Auto")
            : new RowDefinitions("Auto,Auto");
        for (var i = 0; i < _metricTiles.Length; i++)
        {
            Grid.SetColumn(_metricTiles[i], compact ? 0 : i % 2);
            Grid.SetRow(_metricTiles[i], compact ? i : i / 2);
        }
    }

    // The surface and theme wiring lives in GlassWindow; this just repaints the shell from
    // the resolved palette. That is all a new screen has to write, too.
    private void ApplyTheme() => ApplySurface(_selectedBackdrop, _selectedTheme);

    protected override void PaintChrome(GlassPalette p)
    {
        _darkTheme = p.Dark;
        _header.Background = p.Panel;
        _footer.Background = p.Panel;
        // The sidebar keeps a slightly deeper flat shade; on glass it matches the panels.
        _sidebar.Background = p.IsGlass ? p.Panel : Brush(p.Dark ? "#141414" : "#FAFAFA");

        MutedBrush.Color = p.MutedForeground;

        _header.BorderBrush = p.Line;
        _sidebar.BorderBrush = p.Line;
        _footer.BorderBrush = p.Line;

        foreach (var card in _cards)
        {
            card.Background = p.Card;
            card.BorderBrush = p.CardBorder;
            card.BorderThickness = p.CardBorderThickness;
        }

        ApplyInteractivePalette(p);
    }

    private static string VersionLabel()
    {
        var version = FullVersionLabel();
        if (version.Contains("0.2", StringComparison.OrdinalIgnoreCase)) return "PREVIEW 0.2";
        return version.ToUpperInvariant();
    }

    private static string FullVersionLabel()
    {
        var informational = typeof(SettingsWindow).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?
            .Split('+')[0];
        return string.IsNullOrWhiteSpace(informational) ? "Preview" : $"v{informational}";
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private enum SettingsPage
    {
        General,
        Edge,
        Monitor,
        Behavior,
        Startup,
        About
    }
}
