using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using EdgePilot.Core;

namespace EdgePilot.UI;

public sealed class SettingsWindow : Window
{
    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#FF8A3D"));
    private static readonly IBrush AccentSoftBrush = new SolidColorBrush(Color.FromArgb(38, 255, 138, 61));
    private static readonly IBrush PreviewBackgroundBrush = new SolidColorBrush(Color.Parse("#101114"));
    private static readonly IBrush PreviewBorderBrush = new SolidColorBrush(Color.Parse("#404247"));
    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#8D9096"));

    private readonly ComboBox _drive;
    private readonly ComboBox _refresh;
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
    private readonly Dictionary<SettingsPage, Button> _navButtons = new();
    private readonly Dictionary<SettingsPage, Control> _pages = new();
    private readonly List<Border> _cards = new();
    private readonly List<Border> _tiles = new();
    private readonly string? _initialDrive;
    private readonly int[] _intervals = { 500, 1000, 2000, 5000 };

    private NotchPreferences _savedPreferences;
    private EdgeSide _selectedEdge;
    private NotchDisplayMode _selectedMode;
    private HoverSensitivity _selectedSensitivity;
    private SettingsPage _selectedPage;
    private bool _ready;

    public SettingsWindow(NotchPreferences current, Action<NotchPreferences> apply, string? warning = null, string? storagePath = null,
        IReadOnlyList<DriveSnapshot>? drives = null, Action<NotchPreferences>? savePreferences = null,
        Action? exit = null, bool hasTray = false)
    {
        _savedPreferences = current;
        _selectedEdge = current.Edge;
        _selectedMode = current.Mode;
        _selectedSensitivity = current.Sensitivity;
        _initialDrive = current.SelectedDrive;

        Title = "EdgePilot · Impostazioni";
        Icon = AppIcon.Load();
        Width = 900;
        Height = 640;
        MinWidth = 760;
        MinHeight = 560;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = OperatingSystem.IsLinux() ? ThemeVariant.Default : ThemeVariant.Dark;

        _drive = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxDropDownHeight = 280,
            MinWidth = 260
        };
        UpdateDrives(drives ?? Array.Empty<DriveSnapshot>());

        _refresh = new ComboBox
        {
            ItemsSource = new[] { "Ogni 0,5 secondi", "Ogni secondo", "Ogni 2 secondi", "Ogni 5 secondi" },
            SelectedIndex = Array.IndexOf(_intervals, current.RefreshIntervalMs),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 220
        };

        var metricOptions = new[] { VisibleMetrics.Cpu, VisibleMetrics.Memory, VisibleMetrics.Disk, VisibleMetrics.Network };
        var metricNames = new[] { "CPU", "Memoria", "Disco", "Rete" };
        _metrics = metricOptions.Select((flag, index) => new CheckBox
        {
            Content = metricNames[index],
            IsChecked = current.Metrics.HasFlag(flag),
            FontWeight = FontWeight.SemiBold
        }).ToArray();

        _autostart = new CheckBox
        {
            Content = "Avvia EdgePilot all’accesso",
            IsChecked = current.StartAtLogin,
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
            Child = previewGrid
        };

        _edgeButtons =
        [
            SegmentButton("Destra", () => SelectEdge(EdgeSide.Right)),
            SegmentButton("Sinistra", () => SelectEdge(EdgeSide.Left)),
            SegmentButton("Alto", () => SelectEdge(EdgeSide.Top)),
            SegmentButton("Basso", () => SelectEdge(EdgeSide.Bottom))
        ];

        _modeButtons =
        [
            SegmentButton("Hover", () => SelectMode(NotchDisplayMode.Hover)),
            SegmentButton("Sempre aperto", () => SelectMode(NotchDisplayMode.Always)),
            SegmentButton("Nascosto", () => SelectMode(NotchDisplayMode.Hidden))
        ];

        _sensitivityButtons =
        [
            SegmentButton("Precisa", () => SelectSensitivity(HoverSensitivity.Precise)),
            SegmentButton("Normale", () => SelectSensitivity(HoverSensitivity.Normal)),
            SegmentButton("Ampia", () => SelectSensitivity(HoverSensitivity.Wide))
        ];

        _status = new TextBlock
        {
            Text = warning ?? "Nessuna modifica da salvare.",
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        _applyButton = new Button
        {
            Content = "Salva modifiche",
            Background = AccentBrush,
            Padding = new Thickness(18, 9),
            MinWidth = 130,
            IsEnabled = false
        };
        _resetButton = new Button
        {
            Content = "Annulla modifiche",
            Padding = new Thickness(14, 9),
            IsEnabled = false
        };

        _drive.SelectionChanged += (_, _) => MarkDirty();
        _refresh.SelectionChanged += (_, _) => MarkDirty();
        foreach (var metric in _metrics) metric.Click += (_, _) => MarkDirty();
        _autostart.Click += (_, _) => MarkDirty();

        _resetButton.Click += (_, _) => ResetToSaved();
        _applyButton.Click += (_, _) => SaveChanges(apply, savePreferences, storagePath, hasTray);

        _pageHost = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };

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

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("190,*")
        };
        body.Children.Add(_sidebar);
        Grid.SetColumn(_sidebar, 0);
        body.Children.Add(_pageHost);
        Grid.SetColumn(_pageHost, 1);
        shell.Children.Add(body);
        Grid.SetRow(body, 1);

        shell.Children.Add(_footer);
        Grid.SetRow(_footer, 2);
        Content = shell;

        _ready = true;
        SelectEdge(current.Edge, markDirty: false);
        SelectMode(current.Mode, markDirty: false);
        SelectSensitivity(current.Sensitivity, markDirty: false);
        ShowPage(SettingsPage.Edge);
        ApplyTheme();
        ActualThemeVariantChanged += (_, _) => ApplyTheme();
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

    private Border BuildHeader()
    {
        var title = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new Border
                {
                    Width = 7,
                    Height = 32,
                    Background = AccentBrush,
                    CornerRadius = new CornerRadius(4)
                },
                new StackPanel
                {
                    Spacing = 1,
                    Children =
                    {
                        new TextBlock { Text = "EdgePilot", FontSize = 20, FontWeight = FontWeight.SemiBold },
                        new TextBlock { Text = "Impostazioni", FontSize = 12, Foreground = MutedBrush }
                    }
                }
            }
        };

        var badge = new Border
        {
            Background = AccentSoftBrush,
            BorderBrush = AccentBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "PREVIEW 0.1",
                FontSize = 10,
                FontWeight = FontWeight.SemiBold
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(24, 0)
        };
        grid.Children.Add(title);
        grid.Children.Add(badge);
        Grid.SetColumn(badge, 1);

        return new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = grid
        };
    }

    private Border BuildSidebar()
    {
        var nav = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(14, 18)
        };
        nav.Children.Add(new TextBlock
        {
            Text = "SETTINGS",
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = MutedBrush,
            Margin = new Thickness(10, 0, 0, 8)
        });
        nav.Children.Add(NavButton("Edge", SettingsPage.Edge));
        nav.Children.Add(NavButton("Monitor", SettingsPage.Monitor));
        nav.Children.Add(NavButton("Behavior", SettingsPage.Behavior));
        nav.Children.Add(NavButton("Startup", SettingsPage.Startup));
        nav.Children.Add(NavButton("About", SettingsPage.About));

        return new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = nav
        };
    }

    private Border BuildFooter()
    {
        var statusDot = new Border
        {
            Width = 7,
            Height = 7,
            Background = AccentBrush,
            CornerRadius = new CornerRadius(4),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 9, 0)
        };
        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { statusDot, _status }
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _resetButton, _applyButton }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(24, 0)
        };
        grid.Children.Add(statusRow);
        grid.Children.Add(actions);
        Grid.SetColumn(actions, 1);

        return new Border
        {
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = grid
        };
    }

    private Control BuildEdgePage()
    {
        var positionOptions = new StackPanel
        {
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Label("Posizione"),
                Description("Scegli da quale bordo EdgePilot deve emergere."),
                SegmentRow(_edgeButtons)
            }
        };

        var previewLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 24
        };
        previewLayout.Children.Add(_previewFrame);
        previewLayout.Children.Add(positionOptions);
        Grid.SetColumn(positionOptions, 1);

        return Page(
            "Edge",
            "Decidi dove vive EdgePilot e come deve comparire sul desktop.",
            Card(
                SectionTitle("Bordo dello schermo"),
                Description("La preview mostra la posizione reale della linguetta rispetto allo schermo."),
                previewLayout),
            Card(
                SectionTitle("Visualizzazione"),
                Description("Hover resta discreto, Sempre aperto mantiene il pannello visibile, Nascosto lo rimuove dal bordo."),
                SegmentRow(_modeButtons)));
    }

    private Control BuildMonitorPage()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 12
        };

        var descriptions = new[]
        {
            "Utilizzo del processore",
            "Memoria in uso",
            "Spazio del volume selezionato",
            "Download e upload"
        };
        for (var i = 0; i < _metrics.Length; i++)
        {
            var tile = MetricTile(_metrics[i], descriptions[i]);
            grid.Children.Add(tile);
            Grid.SetColumn(tile, i % 2);
            Grid.SetRow(tile, i / 2);
        }

        return Page(
            "Monitor",
            "Scegli quali informazioni del modulo System devono apparire nella notch.",
            Card(
                SectionTitle("Metriche visibili"),
                Description("Mostra solo ciò che ti serve. EdgePilot adatta automaticamente l’altezza del pannello."),
                grid),
            Card(
                SectionTitle("Disco"),
                Description("Seleziona il volume usato dalla metrica Disco. I volumi montati vengono aggiornati automaticamente."),
                _drive));
    }

    private Control BuildBehaviorPage()
    {
        return Page(
            "Behavior",
            "Regola frequenza dei dati e sensibilità dell’interazione con il bordo.",
            Card(
                SectionTitle("Aggiornamento dei dati"),
                Description("Una frequenza più alta rende i valori più reattivi; una più bassa riduce il lavoro in background."),
                _refresh),
            Card(
                SectionTitle("Sensibilità di apertura"),
                Description("Precisa richiede di arrivare vicino alla linguetta. Ampia rende più facile intercettare EdgePilot passando vicino al bordo."),
                SegmentRow(_sensitivityButtons)));
    }

    private Control BuildStartupPage(Action? exit)
    {
        var exitButton = new Button
        {
            Content = "Esci da EdgePilot",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(14, 8)
        };
        exitButton.Click += (_, _) =>
        {
            if (exit is not null) exit();
            else Close();
        };

        return Page(
            "Startup",
            "Controlla come EdgePilot si avvia e rimane disponibile nella sessione desktop.",
            Card(
                SectionTitle("Avvio automatico"),
                Description("Avvia EdgePilot per l’utente corrente quando accedi al sistema."),
                _autostart),
            Card(
                SectionTitle("Sessione"),
                Description("Chiude completamente EdgePilot e il processo in esecuzione."),
                exitButton));
    }

    private Control BuildAboutPage()
    {
        return Page(
            "About",
            "Informazioni sul progetto e sui principi della preview corrente.",
            Card(
                new TextBlock
                {
                    Text = "EdgePilot",
                    FontSize = 26,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = "Your desktop has edges. EdgePilot makes them useful.",
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap
                },
                Description("Early preview · Windows + Linux · Avalonia + .NET"),
                Divider(),
                new TextBlock
                {
                    Text = "Local first",
                    FontWeight = FontWeight.SemiBold
                },
                Description("Nessun account, nessun backend cloud e nessun uploader di telemetria. Le metriche vengono lette sul computer locale.")));
    }

    private ScrollViewer Page(string title, string subtitle, params Control[] sections)
    {
        var stack = new StackPanel
        {
            Margin = new Thickness(30, 26, 30, 30),
            Spacing = 16
        };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 28,
            FontWeight = FontWeight.SemiBold
        });
        stack.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = MutedBrush,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, -8, 0, 8)
        });
        foreach (var section in sections) stack.Children.Add(section);
        return new ScrollViewer { Content = stack };
    }

    private Border Card(params Control[] children)
    {
        var stack = new StackPanel { Spacing = 12 };
        foreach (var child in children) stack.Children.Add(child);
        var card = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(18),
            Child = stack
        };
        _cards.Add(card);
        return card;
    }

    private Border MetricTile(CheckBox checkbox, string description)
    {
        var tile = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(14),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    checkbox,
                    Description(description)
                }
            }
        };
        _tiles.Add(tile);
        return tile;
    }

    private static TextBlock SectionTitle(string text) => new()
    {
        Text = text,
        FontSize = 16,
        FontWeight = FontWeight.SemiBold
    };

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        FontWeight = FontWeight.SemiBold
    };

    private static TextBlock Description(string text) => new()
    {
        Text = text,
        Foreground = MutedBrush,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap
    };

    private static Border Divider() => new()
    {
        Height = 1,
        Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)),
        Margin = new Thickness(0, 4)
    };

    private static StackPanel SegmentRow(IEnumerable<Button> buttons)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        foreach (var button in buttons) row.Children.Add(button);
        return row;
    }

    private static Button SegmentButton(string text, Action select)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(13, 8),
            MinWidth = 72
        };
        button.Click += (_, _) => select();
        return button;
    }

    private Button NavButton(string text, SettingsPage page)
    {
        var button = new Button
        {
            Content = text,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 10),
            BorderThickness = new Thickness(0)
        };
        button.Click += (_, _) => ShowPage(page);
        _navButtons[page] = button;
        return button;
    }

    private void ShowPage(SettingsPage page)
    {
        _selectedPage = page;
        _pageHost.Content = _pages[page];
        foreach (var item in _navButtons)
        {
            var selected = item.Key == page;
            item.Value.Background = selected ? AccentSoftBrush : Brushes.Transparent;
            item.Value.BorderBrush = selected ? AccentBrush : Brushes.Transparent;
            item.Value.BorderThickness = selected ? new Thickness(3, 0, 0, 0) : new Thickness(0);
        }
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

    private static void UpdateSegments(IReadOnlyList<Button> buttons, int selectedIndex)
    {
        for (var i = 0; i < buttons.Count; i++)
        {
            var selected = i == selectedIndex;
            buttons[i].Background = selected ? AccentBrush : Brushes.Transparent;
            buttons[i].BorderBrush = selected ? AccentBrush : new SolidColorBrush(Color.FromArgb(70, 128, 128, 128));
            buttons[i].BorderThickness = new Thickness(1);
        }
    }

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

    private VisibleMetrics SelectedMetrics()
    {
        var options = new[] { VisibleMetrics.Cpu, VisibleMetrics.Memory, VisibleMetrics.Disk, VisibleMetrics.Network };
        VisibleMetrics selected = 0;
        for (var i = 0; i < _metrics.Length; i++)
            if (_metrics[i].IsChecked == true) selected |= options[i];
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
            StartAtLogin = _autostart.IsChecked == true
        };
    }

    private void MarkDirty()
    {
        if (!_ready) return;
        var current = CurrentPreferences();
        var dirty = current != _savedPreferences;
        _applyButton.IsEnabled = dirty;
        _resetButton.IsEnabled = dirty;
        if (dirty) _status.Text = "Modifiche non salvate.";
        else _status.Text = "Nessuna modifica da salvare.";
    }

    private void ResetToSaved()
    {
        _ready = false;
        _selectedEdge = _savedPreferences.Edge;
        _selectedMode = _savedPreferences.Mode;
        _selectedSensitivity = _savedPreferences.Sensitivity;
        _refresh.SelectedIndex = Array.IndexOf(_intervals, _savedPreferences.RefreshIntervalMs);
        var options = new[] { VisibleMetrics.Cpu, VisibleMetrics.Memory, VisibleMetrics.Disk, VisibleMetrics.Network };
        for (var i = 0; i < _metrics.Length; i++) _metrics[i].IsChecked = _savedPreferences.Metrics.HasFlag(options[i]);
        _autostart.IsChecked = _savedPreferences.StartAtLogin;
        if (_drive.ItemsSource is IReadOnlyList<DriveChoice> choices)
            _drive.SelectedItem = choices.First(x => DriveSelection.PathComparer.Equals(x.Name, _savedPreferences.SelectedDrive));
        UpdateSegments(_edgeButtons, (int)_selectedEdge);
        UpdateSegments(_modeButtons, (int)_selectedMode);
        UpdateSegments(_sensitivityButtons, (int)_selectedSensitivity);
        UpdatePreview();
        _ready = true;
        _applyButton.IsEnabled = false;
        _resetButton.IsEnabled = false;
        _status.Text = "Modifiche annullate.";
    }

    private void SaveChanges(Action<NotchPreferences> apply, Action<NotchPreferences>? savePreferences, string? storagePath, bool hasTray)
    {
        var selectedMetrics = SelectedMetrics();
        if (selectedMetrics == 0)
        {
            _status.Text = "Seleziona almeno una metrica da mostrare.";
            ShowPage(SettingsPage.Monitor);
            return;
        }

        var value = CurrentPreferences();
        try
        {
            if (savePreferences is not null) savePreferences(value);
            else PreferenceStore.Save(storagePath ?? PreferenceStore.DefaultPath, value);
            apply(value);
            _savedPreferences = value;
            _applyButton.IsEnabled = false;
            _resetButton.IsEnabled = false;
            _status.Text = value.Mode == NotchDisplayMode.Hidden
                ? (hasTray
                    ? "Pannello nascosto. Puoi riaprirlo dall’area di notifica."
                    : "Pannello nascosto. Chiudendo le impostazioni esci da EdgePilot; al prossimo avvio tornerai qui.")
                : "Impostazioni salvate.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            _status.Text = "Impossibile salvare le impostazioni. Verifica permessi e spazio disponibile, poi riprova.";
            _applyButton.IsEnabled = true;
        }
    }

    private void ApplyTheme()
    {
        var dark = ActualThemeVariant == ThemeVariant.Dark;
        Background = Brush(dark ? "#121212" : "#F4F4F5");
        _header.Background = Brush(dark ? "#151515" : "#FFFFFF");
        _sidebar.Background = Brush(dark ? "#141414" : "#FAFAFA");
        _footer.Background = Brush(dark ? "#151515" : "#FFFFFF");

        var line = Brush(dark ? "#2C2C2C" : "#DDDDDF");
        _header.BorderBrush = line;
        _sidebar.BorderBrush = line;
        _footer.BorderBrush = line;

        foreach (var card in _cards)
        {
            card.Background = Brush(dark ? "#191919" : "#FFFFFF");
            card.BorderBrush = Brush(dark ? "#2D2D2D" : "#E2E2E4");
        }
        foreach (var tile in _tiles)
        {
            tile.Background = Brush(dark ? "#1F1F1F" : "#F8F8F9");
            tile.BorderBrush = Brush(dark ? "#343434" : "#E6E6E8");
        }
        ShowPage(_selectedPage);
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private enum SettingsPage
    {
        Edge,
        Monitor,
        Behavior,
        Startup,
        About
    }
}
