using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EdgePilot.Core;

namespace EdgePilot.UI;

public sealed partial class SettingsWindow
{
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
                        new TextBlock
                        {
                            Text = Localization.T("settings.headerSubtitle"),
                            FontSize = 12,
                            Foreground = MutedBrush
                        }
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
            Text = Localization.T("settings.sidebarHeading"),
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = MutedBrush,
            Margin = new Thickness(10, 0, 0, 8)
        });
        nav.Children.Add(NavButton("⚙", Localization.T("nav.general"), SettingsPage.General));
        nav.Children.Add(NavButton("◨", Localization.T("nav.edge"), SettingsPage.Edge));
        nav.Children.Add(NavButton("▦", Localization.T("nav.monitor"), SettingsPage.Monitor));
        nav.Children.Add(NavButton("◎", Localization.T("nav.behavior"), SettingsPage.Behavior));
        nav.Children.Add(NavButton("↻", Localization.T("nav.startup"), SettingsPage.Startup));
        nav.Children.Add(NavButton("ⓘ", Localization.T("nav.about"), SettingsPage.About));

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

    private Control BuildGeneralPage()
    {
        // Mica/Acrylic are Windows 11 only; the selector is hidden elsewhere.
        _surfaceCard = Card(
            SectionTitle("▦", Localization.T("general.surfaceTitle")),
            Description(Localization.T("general.surfaceDescription")),
            SegmentRow(_backdropButtons));
        _surfaceCard.IsVisible = OperatingSystem.IsWindows();

        return Page(
            Localization.T("general.pageTitle"),
            Localization.T("general.pageSubtitle"),
            Card(
                SectionTitle("文", Localization.T("general.languageTitle")),
                Description(Localization.T("general.languageDescription")),
                _language),
            Card(
                SectionTitle("◐", Localization.T("general.themeTitle")),
                Description(Localization.T("general.themeDescription")),
                SegmentRow(_themeButtons)),
            _surfaceCard);
    }

    private Control BuildEdgePage()
    {
        _positionOptions = new StackPanel
        {
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Label(Localization.T("edge.positionLabel")),
                Description(Localization.T("edge.positionHint")),
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
            Localization.T("edge.pageTitle"),
            Localization.T("edge.pageSubtitle"),
            Card(
                SectionTitle("◨", Localization.T("edge.positionTitle")),
                Description(Localization.T("edge.positionDescription")),
                _previewLayout),
            Card(
                SectionTitle("◌", Localization.T("edge.behaviorTitle")),
                Description(Localization.T("edge.behaviorDescription")),
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
            Localization.T("metric.cpu.description"),
            Localization.T("metric.memory.description"),
            Localization.T("metric.disk.description"),
            Localization.T("metric.network.description")
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
            SectionTitle("▱", Localization.T("monitor.diskTitle")),
            Description(Localization.T("monitor.diskDescription")),
            _drive);

        return Page(
            Localization.T("monitor.pageTitle"),
            Localization.T("monitor.pageSubtitle"),
            Card(
                SectionTitle("▦", Localization.T("monitor.metricsTitle")),
                Description(Localization.T("monitor.metricsDescription")),
                _metricGrid),
            _diskCard);
    }

    private Control BuildBehaviorPage()
    {
        return Page(
            Localization.T("behavior.pageTitle"),
            Localization.T("behavior.pageSubtitle"),
            Card(
                SectionTitle("↻", Localization.T("behavior.refreshTitle")),
                Description(Localization.T("behavior.refreshDescription")),
                SettingField(Localization.T("behavior.refreshLabel"), _refresh)),
            Card(
                SectionTitle("◎", Localization.T("behavior.sensitivityTitle")),
                Description(Localization.T("behavior.sensitivityDescription")),
                SegmentRow(_sensitivityButtons)));
    }

    private Control BuildStartupPage(Action? exit)
    {
        var exitButton = new Button
        {
            Content = ButtonContent("⏻", Localization.T("startup.exitButton")),
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(14, 8)
        };
        exitButton.Click += (_, _) =>
        {
            if (exit is not null) exit();
            else Close();
        };

        return Page(
            Localization.T("startup.pageTitle"),
            Localization.T("startup.pageSubtitle"),
            Card(
                SectionTitle("↻", Localization.T("startup.autostartTitle")),
                Description(Localization.T("startup.autostartDescription")),
                _autostart),
            Card(
                SectionTitle("⏻", Localization.T("startup.sessionTitle")),
                Description(Localization.T("startup.sessionDescription")),
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
                Description(Localization.T("about.productDescription")),
                ChipRow("Open source", "MIT", "Windows + Linux", "Local-first")
            }
        };
        hero.Children.Add(heroText);
        Grid.SetColumn(heroText, 1);

        var repoButton = LinkButton(Localization.T("about.repoButton"), "https://github.com/pricootz/edgepilot");
        var profileButton = LinkButton(Localization.T("about.profileButton"), "https://github.com/pricootz");

        return Page(
            Localization.T("about.pageTitle"),
            Localization.T("about.pageSubtitle"),
            Card(hero),
            Card(
                SectionTitle("✦", Localization.T("about.authorTitle")),
                new TextBlock
                {
                    Text = Localization.T("about.authorLine"),
                    FontSize = 18,
                    FontWeight = FontWeight.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                },
                Description(Localization.T("about.authorDescription")),
                SegmentRow(new[] { repoButton, profileButton })),
            Card(
                SectionTitle("⌁", Localization.T("about.philosophyTitle")),
                FeatureRow("◉", Localization.T("about.localTitle"), Localization.T("about.localDescription")),
                Divider(),
                FeatureRow("◨", Localization.T("about.edgeNativeTitle"), Localization.T("about.edgeNativeDescription")),
                Divider(),
                FeatureRow("◇", Localization.T("about.evolvingTitle"), Localization.T("about.evolvingDescription"))),
            Card(
                SectionTitle("ⓘ", Localization.T("about.versionTitle")),
                new TextBlock { Text = FullVersionLabel(), FontWeight = FontWeight.SemiBold },
                Description(Localization.T("about.versionDescription"))));
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
}