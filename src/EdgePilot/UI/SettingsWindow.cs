using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    private static readonly VisibleMetrics[] MetricOptions =
        { VisibleMetrics.Cpu, VisibleMetrics.Memory, VisibleMetrics.Disk, VisibleMetrics.Network };

    private const string GitHubMarkPath =
        "M6.766 11.328c-2.063-.25-3.516-1.734-3.516-3.656 0-.781.281-1.625.75-2.188-.203-.515-.172-1.609.063-2.062.625-.078 1.468.25 1.968.703.594-.187 1.219-.281 1.985-.281.765 0 1.39.094 1.953.265.484-.437 1.344-.765 1.969-.687.218.422.25 1.515.046 2.047.5.593.766 1.39.766 2.203 0 1.922-1.453 3.375-3.547 3.64.531.344.89 1.094.89 1.954v1.625c0 .468.391.734.86.547C13.781 14.359 16 11.53 16 8.03 16 3.61 12.406 0 7.984 0 3.563 0 0 3.61 0 8.031a7.88 7.88 0 0 0 5.172 7.422c.422.156.828-.125.828-.547v-1.25c-.219.094-.5.156-.75.156-1.031 0-1.64-.562-2.078-1.609-.172-.422-.36-.672-.719-.719-.187-.015-.25-.093-.25-.187 0-.188.313-.328.625-.328.453 0 .844.281 1.25.86.313.452.64.655 1.031.655s.641-.14 1-.5c.266-.265.47-.5.657-.656";

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
    private readonly List<StackPanel> _pageStacks = new();
    private readonly string? _initialDrive;
    private readonly int[] _intervals = { 500, 1000, 2000, 5000 };

    private Border _diskCard = null!;
    private Border[] _metricTiles = Array.Empty<Border>();
    private Grid _body = null!;
    private Grid _previewLayout = null!;
    private StackPanel _positionOptions = null!;
    private Grid _metricGrid = null!;
    private Border _versionBadge = null!;

    private NotchPreferences _savedPreferences;
    private EdgeSide _selectedEdge;
    private NotchDisplayMode _selectedMode;
    private HoverSensitivity _selectedSensitivity;
    private SettingsPage _selectedPage;
    private bool _ready;
    private bool _darkTheme;

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
        Width = 920;
        Height = 680;
        MinWidth = 680;
        MinHeight = 520;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = OperatingSystem.IsLinux() ? ThemeVariant.Default : ThemeVariant.Dark;

        _drive = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxDropDownHeight = 280,
            MaxWidth = 460
        };
        UpdateDrives(drives ?? Array.Empty<DriveSnapshot>());

        _refresh = new ComboBox
        {
            ItemsSource = new[] { "Ogni 0,5 secondi", "Ogni secondo", "Ogni 2 secondi", "Ogni 5 secondi" },
            SelectedIndex = Array.IndexOf(_intervals, current.RefreshIntervalMs),
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = 320,
            MinWidth = 210
        };

        var metricNames = new[] { "CPU", "Memoria", "Disco", "Rete" };
        _metrics = MetricOptions.Select((flag, index) => new CheckBox
        {
            Content = metricNames[index],
            IsChecked = current.Metrics.HasFlag(flag),
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        }).ToArray();

        _autostart = new CheckBox
        {
            Content = "Avvia EdgePilot quando accedo al sistema",
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
            Child = previewGrid,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _edgeButtons =
        [
            SegmentButton("▸  Destra", () => SelectEdge(EdgeSide.Right)),
            SegmentButton("◂  Sinistra", () => SelectEdge(EdgeSide.Left)),
            SegmentButton("▴  Alto", () => SelectEdge(EdgeSide.Top)),
            SegmentButton("▾  Basso", () => SelectEdge(EdgeSide.Bottom))
        ];

        _modeButtons =
        [
            SegmentButton("◌  Al passaggio", () => SelectMode(NotchDisplayMode.Hover)),
            SegmentButton("●  Sempre visibile", () => SelectMode(NotchDisplayMode.Always)),
            SegmentButton("○  Nascosto", () => SelectMode(NotchDisplayMode.Hidden))
        ];

        _sensitivityButtons =
        [
            SegmentButton("Precisa", () => SelectSensitivity(HoverSensitivity.Precise)),
            SegmentButton("Normale", () => SelectSensitivity(HoverSensitivity.Normal)),
            SegmentButton("Ampia", () => SelectSensitivity(HoverSensitivity.Wide))
        ];

        _status = new TextBlock
        {
            Text = warning ?? "Tutto salvato.",
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            MaxLines = 2
        };

        _applyButton = new Button
        {
            Content = ButtonContent("✓", "Salva modifiche"),
            Background = AccentBrush,
            Padding = new Thickness(17, 9),
            MinWidth = 146,
            IsEnabled = false
        };
        _resetButton = new Button
        {
            Content = ButtonContent("↶", "Annulla"),
            Padding = new Thickness(14, 9),
            MinWidth = 100,
            IsEnabled = false
        };

        _drive.SelectionChanged += (_, _) => MarkDirty();
        _refresh.SelectionChanged += (_, _) => MarkDirty();
        foreach (var metric in _metrics) metric.Click += (_, _) => OnMetricChanged();
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
        UpdateConditionalSettings();
        ShowPage(SettingsPage.Edge);
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

    private Border BuildHeader()
    {
        var title = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new EdgePilotLogo { Width = 36, Height = 36 },
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

        _versionBadge = new Border
        {
            Background = AccentSoftBrush,
            BorderBrush = AccentBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = VersionLabel(),
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
        grid.Children.Add(_versionBadge);
        Grid.SetColumn(_versionBadge, 1);

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
            Margin = new Thickness(12, 18)
        };
        nav.Children.Add(new TextBlock
        {
            Text = "IMPOSTAZIONI",
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = MutedBrush,
            Margin = new Thickness(10, 0, 0, 8)
        });
        nav.Children.Add(NavButton("◨", "Bordo", SettingsPage.Edge));
        nav.Children.Add(NavButton("▦", "Monitor", SettingsPage.Monitor));
        nav.Children.Add(NavButton("⚙", "Comportamento", SettingsPage.Behavior));
        nav.Children.Add(NavButton("↻", "Avvio", SettingsPage.Startup));
        nav.Children.Add(NavButton("ⓘ", "Informazioni", SettingsPage.About));

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
        _positionOptions = new StackPanel
        {
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Label("Posizione"),
                Description("Scegli il bordo da cui EdgePilot deve emergere."),
                SegmentRow(_edgeButtons)
            }
        };

        _previewLayout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            ColumnSpacing = 24
        };
        _previewLayout.Children.Add(_previewFrame);
        _previewLayout.Children.Add(_positionOptions);
        Grid.SetColumn(_positionOptions, 1);

        return Page(
            "Bordo e presenza",
            "Definisci dove vive EdgePilot e quanto deve essere presente sul desktop.",
            Card(
                SectionTitle("◨", "Posizione sullo schermo"),
                Description("La preview cambia insieme alla scelta e mostra dove comparirà la linguetta."),
                _previewLayout),
            Card(
                SectionTitle("◌", "Comportamento del pannello"),
                Description("Al passaggio mantiene EdgePilot discreto. Sempre visibile lo lascia aperto. Nascosto lo rimuove dal bordo finché non lo riapri dalla tray o dalle impostazioni."),
                SegmentRow(_modeButtons)));
    }

    private Control BuildMonitorPage()
    {
        _metricGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 12
        };

        var descriptions = new[]
        {
            "Carico del processore in tempo reale",
            "Memoria RAM attualmente utilizzata",
            "Spazio occupato sul volume scelto",
            "Traffico di download e upload"
        };
        var icons = new[] { "◉", "▤", "▱", "↕" };
        _metricTiles = new Border[_metrics.Length];
        for (var i = 0; i < _metrics.Length; i++)
        {
            var tile = MetricTile(_metrics[i], icons[i], descriptions[i]);
            _metricTiles[i] = tile;
            _metricGrid.Children.Add(tile);
            Grid.SetColumn(tile, i % 2);
            Grid.SetRow(tile, i / 2);
        }

        _diskCard = Card(
            SectionTitle("▱", "Volume per la metrica Disco"),
            Description("Questa scelta compare solo quando la metrica Disco è attiva. EdgePilot conserva comunque la selezione se la disattivi temporaneamente."),
            _drive);

        return Page(
            "Monitor di sistema",
            "Decidi quali dati del modulo System devono comparire nella notch.",
            Card(
                SectionTitle("▦", "Metriche visibili"),
                Description("Fai clic sull’intera scheda per attivare o disattivare una metrica. La notch si ridimensiona automaticamente."),
                _metricGrid),
            _diskCard);
    }

    private Control BuildBehaviorPage()
    {
        return Page(
            "Comportamento",
            "Regola quanto spesso EdgePilot aggiorna i dati e quanto facilmente reagisce al bordo.",
            Card(
                SectionTitle("↻", "Frequenza di aggiornamento"),
                Description("Un intervallo breve rende i valori più reattivi. Un intervallo più lungo riduce leggermente il lavoro in background."),
                SettingField("Aggiorna i dati", _refresh)),
            Card(
                SectionTitle("◎", "Sensibilità al bordo"),
                Description("Precisa richiede di arrivare vicino alla linguetta. Ampia crea una zona di aggancio più generosa. Normale è il compromesso consigliato."),
                SegmentRow(_sensitivityButtons)));
    }

    private Control BuildStartupPage(Action? exit)
    {
        var exitButton = new Button
        {
            Content = ButtonContent("⏻", "Esci da EdgePilot"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(14, 8)
        };
        exitButton.Click += (_, _) =>
        {
            if (exit is not null) exit();
            else Close();
        };

        return Page(
            "Avvio e sessione",
            "Scegli come EdgePilot entra nella sessione e quando deve terminare completamente.",
            Card(
                SectionTitle("↻", "Avvio automatico"),
                Description("Abilita l’avvio per l’utente corrente. EdgePilot resterà disponibile senza doverlo lanciare manualmente a ogni accesso."),
                _autostart),
            Card(
                SectionTitle("⏻", "Sessione corrente"),
                Description("Chiude completamente EdgePilot, inclusi pannello e processo in background."),
                exitButton));
    }

    private Control BuildAboutPage()
    {
        var hero = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 20
        };
        var logo = new EdgePilotLogo
        {
            Width = 76,
            Height = 76,
            VerticalAlignment = VerticalAlignment.Top
        };
        hero.Children.Add(logo);

        var heroText = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock
                {
                    Text = "EdgePilot",
                    FontSize = 28,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = "Your desktop has edges. EdgePilot makes them useful.",
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap
                },
                Description("Una superficie desktop edge-native per Windows e Linux, progettata per mostrare informazioni e azioni solo quando servono."),
                ChipRow("Open source", "MIT", "Windows + Linux", "Local-first")
            }
        };
        hero.Children.Add(heroText);
        Grid.SetColumn(heroText, 1);

        var repoButton = LinkButton("Repository GitHub", "https://github.com/pricootz/edgepilot");
        var profileButton = LinkButton("Profilo @pricootz", "https://github.com/pricootz");

        return Page(
            "Informazioni",
            "Il progetto, chi lo sviluppa e i principi su cui viene costruito.",
            Card(hero),
            Card(
                SectionTitle("✦", "Autore"),
                new TextBlock
                {
                    Text = "Creato e mantenuto da @pricootz",
                    FontSize = 18,
                    FontWeight = FontWeight.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                },
                Description("EdgePilot è un progetto indipendente open source. Idee, sviluppo, direzione del prodotto e manutenzione principale fanno capo a @pricootz, con contributi della community tramite GitHub."),
                SegmentRow(new[] { repoButton, profileButton })),
            Card(
                SectionTitle("⌁", "Filosofia del progetto"),
                FeatureRow("◉", "Locale per impostazione predefinita", "Nessun account, nessun backend cloud obbligatorio e nessun uploader di telemetria."),
                Divider(),
                FeatureRow("◨", "Edge-native", "EdgePilot vive sul bordo dello schermo e punta a ridurre finestre, dashboard e notifiche tradizionali."),
                Divider(),
                FeatureRow("◇", "In evoluzione", "La preview attuale parte dal monitor di sistema; i prossimi moduli allargheranno il concetto senza trasformarlo in un pannello generico.")),
            Card(
                SectionTitle("ⓘ", "Versione"),
                new TextBlock { Text = FullVersionLabel(), FontWeight = FontWeight.SemiBold },
                Description("Preview in sviluppo attivo. Le API, il layout e alcune funzioni possono ancora cambiare prima della prima release stabile.")));
    }

    private ScrollViewer Page(string title, string subtitle, params Control[] sections)
    {
        var stack = new StackPanel
        {
            Margin = new Thickness(28, 24, 28, 28),
            Spacing = 16
        };
        _pageStacks.Add(stack);
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
        return new ScrollViewer
        {
            Content = stack,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
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

    private Border MetricTile(CheckBox checkbox, string icon, string description)
    {
        var heading = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 9
        };
        heading.Children.Add(new TextBlock
        {
            Text = icon,
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = AccentBrush
        });
        var title = new TextBlock
        {
            Text = checkbox.Content?.ToString(),
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        heading.Children.Add(title);
        Grid.SetColumn(title, 1);
        checkbox.Content = null;
        heading.Children.Add(checkbox);
        Grid.SetColumn(checkbox, 2);

        var tile = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(14),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = new StackPanel
            {
                Spacing = 7,
                Children =
                {
                    heading,
                    Description(description)
                }
            }
        };
        tile.PointerPressed += (_, e) =>
        {
            if (e.Source is CheckBox) return;
            checkbox.IsChecked = checkbox.IsChecked != true;
            OnMetricChanged();
            e.Handled = true;
        };
        return tile;
    }

    private static Control SettingField(string label, Control control)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(Label(label));
        stack.Children.Add(control);
        return stack;
    }

    private static TextBlock SectionTitle(string icon, string text) => new()
    {
        Text = $"{icon}  {text}",
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

    private static WrapPanel SegmentRow(IEnumerable<Button> buttons)
    {
        var row = new WrapPanel
        {
            Orientation = Orientation.Horizontal
        };
        foreach (var button in buttons)
        {
            button.Margin = new Thickness(0, 0, 8, 8);
            row.Children.Add(button);
        }
        return row;
    }

    private static WrapPanel ChipRow(params string[] labels)
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var label in labels)
        {
            row.Children.Add(new Border
            {
                Background = AccentSoftBrush,
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(9, 4),
                Margin = new Thickness(0, 4, 7, 0),
                Child = new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeight.SemiBold }
            });
        }
        return row;
    }

    private static StackPanel FeatureRow(string icon, string title, string description)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = icon,
                    FontSize = 18,
                    Foreground = AccentBrush,
                    Width = 24,
                    VerticalAlignment = VerticalAlignment.Top
                },
                new StackPanel
                {
                    Spacing = 3,
                    Children =
                    {
                        new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap },
                        Description(description)
                    }
                }
            }
        };
    }

    private static StackPanel ButtonContent(string icon, string text)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7,
            Children =
            {
                new TextBlock { Text = icon, VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
            }
        };
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

    private Button NavButton(string icon, string text, SettingsPage page)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 9,
            Children =
            {
                new TextBlock { Text = icon, Width = 20, TextAlignment = TextAlignment.Center },
                new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }
            }
        };
        var button = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(11, 10),
            BorderThickness = new Thickness(0)
        };
        button.Click += (_, _) => ShowPage(page);
        _navButtons[page] = button;
        return button;
    }

    private Button LinkButton(string text, string url)
    {
        var button = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new PathIcon { Data = Geometry.Parse(GitHubMarkPath), Width = 16, Height = 16 },
                    new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center }
                }
            },
            Padding = new Thickness(13, 8)
        };
        button.Click += (_, _) => OpenUrl(url);
        return button;
    }

    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            _status.Text = "Impossibile aprire il browser. Visita github.com/pricootz/edgepilot.";
        }
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

    private void UpdateMetricTileStates()
    {
        if (_metricTiles.Length == 0) return;
        for (var i = 0; i < _metricTiles.Length; i++)
        {
            var selected = _metrics[i].IsChecked == true;
            _metricTiles[i].Background = selected
                ? AccentSoftBrush
                : Brush(_darkTheme ? "#1F1F1F" : "#F8F8F9");
            _metricTiles[i].BorderBrush = selected
                ? AccentBrush
                : Brush(_darkTheme ? "#343434" : "#E6E6E8");
        }
    }

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
        _status.Text = dirty ? "Modifiche non salvate." : "Tutto salvato.";
    }

    private void ResetToSaved()
    {
        _ready = false;
        _selectedEdge = _savedPreferences.Edge;
        _selectedMode = _savedPreferences.Mode;
        _selectedSensitivity = _savedPreferences.Sensitivity;
        _refresh.SelectedIndex = Array.IndexOf(_intervals, _savedPreferences.RefreshIntervalMs);
        for (var i = 0; i < _metrics.Length; i++) _metrics[i].IsChecked = _savedPreferences.Metrics.HasFlag(MetricOptions[i]);
        _autostart.IsChecked = _savedPreferences.StartAtLogin;
        if (_drive.ItemsSource is IReadOnlyList<DriveChoice> choices)
            _drive.SelectedItem = choices.First(x => DriveSelection.PathComparer.Equals(x.Name, _savedPreferences.SelectedDrive));
        UpdateSegments(_edgeButtons, (int)_selectedEdge);
        UpdateSegments(_modeButtons, (int)_selectedMode);
        UpdateSegments(_sensitivityButtons, (int)_selectedSensitivity);
        UpdatePreview();
        UpdateConditionalSettings();
        UpdateMetricTileStates();
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

    private void ApplyTheme()
    {
        _darkTheme = ActualThemeVariant == ThemeVariant.Dark;
        Background = Brush(_darkTheme ? "#121212" : "#F4F4F5");
        _header.Background = Brush(_darkTheme ? "#151515" : "#FFFFFF");
        _sidebar.Background = Brush(_darkTheme ? "#141414" : "#FAFAFA");
        _footer.Background = Brush(_darkTheme ? "#151515" : "#FFFFFF");

        var line = Brush(_darkTheme ? "#2C2C2C" : "#DDDDDF");
        _header.BorderBrush = line;
        _sidebar.BorderBrush = line;
        _footer.BorderBrush = line;

        foreach (var card in _cards)
        {
            card.Background = Brush(_darkTheme ? "#191919" : "#FFFFFF");
            card.BorderBrush = Brush(_darkTheme ? "#2D2D2D" : "#E2E2E4");
        }
        UpdateMetricTileStates();
        ShowPage(_selectedPage);
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
        Edge,
        Monitor,
        Behavior,
        Startup,
        About
    }
}
