using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using EdgePilot.Core;

namespace EdgePilot.UI;

public sealed class SettingsWindow : Window
{
    private sealed record LanguageChoice(Language? Value, string Caption)
    {
        public override string ToString() => Caption;
    }

    private readonly ComboBox _drive;
    private readonly string? _initialDrive;
    public SettingsWindow(NotchPreferences current, Action<NotchPreferences> apply, string? warning = null, string? storagePath = null,
        IReadOnlyList<DriveSnapshot>? drives = null, Action<NotchPreferences>? savePreferences = null,
        Action? exit = null, bool hasTray = false, Action? reopen = null)
    {
        Title = Localization.T("settings.title");
        Width = 480;
        MinWidth = 420;
        MinHeight = 400;
        RequestedThemeVariant = OperatingSystem.IsLinux() ? Avalonia.Styling.ThemeVariant.Default : Avalonia.Styling.ThemeVariant.Dark;
        Height = 650;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var edge = new ComboBox
        {
            ItemsSource = new[] { Localization.T("edge.right"), Localization.T("edge.left"), Localization.T("edge.top"), Localization.T("edge.bottom") },
            SelectedIndex = (int)current.Edge,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var mode = new ComboBox
        {
            ItemsSource = new[] { Localization.T("mode.hover"), Localization.T("mode.always"), Localization.T("mode.hidden") },
            SelectedIndex = (int)current.Mode,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var intervals = new[] { 500, 1000, 2000, 5000 };
        var refresh = new ComboBox
        {
            ItemsSource = new[] { Localization.T("refresh.0_5s"), Localization.T("refresh.1s"), Localization.T("refresh.2s"), Localization.T("refresh.5s") },
            SelectedIndex = Array.IndexOf(intervals, current.RefreshIntervalMs),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var sensitivity = new ComboBox
        {
            ItemsSource = new[] { Localization.T("sensitivity.precise"), Localization.T("sensitivity.normal"), Localization.T("sensitivity.wide") },
            SelectedIndex = (int)current.Sensitivity,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var metricOptions = new[] { VisibleMetrics.Cpu, VisibleMetrics.Memory, VisibleMetrics.Disk, VisibleMetrics.Network };
        var metricNames = new[] { Localization.T("metric.cpu"), Localization.T("metric.memory"), Localization.T("metric.disk"), Localization.T("metric.network") };
        var metrics = metricOptions.Select((flag, index) => new CheckBox
        {
            Content = metricNames[index], IsChecked = current.Metrics.HasFlag(flag)
        }).ToArray();
        var metricPanel = new WrapPanel();
        foreach (var metric in metrics)
        {
            metric.Margin = new Thickness(0, 0, 16, 0);
            metricPanel.Children.Add(metric);
        }
        var languageChoices = new[]
        {
            new LanguageChoice(null, Localization.T("language.auto")),
            new LanguageChoice(Language.Italian, "Italiano"),
            new LanguageChoice(Language.English, "English"),
            new LanguageChoice(Language.French, "Français"),
        };
        var language = new ComboBox
        {
            ItemsSource = languageChoices,
            SelectedIndex = Array.FindIndex(languageChoices, c => c.Value == current.Language),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _initialDrive = current.SelectedDrive;
        _drive = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxDropDownHeight = 280
        };
        UpdateDrives(drives ?? Array.Empty<DriveSnapshot>());
        var autostart = new CheckBox { Content = Localization.T("settings.autostart"), IsChecked = current.StartAtLogin };
        var exitButton = new Button { Content = Localization.T("settings.exitButton") };
        exitButton.Click += (_, _) => { if (exit is not null) exit(); else Close(); };
        var message = new TextBlock
        {
            Text = warning ?? Localization.T("settings.applyHint"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        var applyButton = new Button { Content = Localization.T("settings.apply"), HorizontalAlignment = HorizontalAlignment.Right };
        applyButton.Click += (_, _) =>
        {
            VisibleMetrics selected = 0;
            for (var i = 0; i < metrics.Length; i++)
                if (metrics[i].IsChecked == true) selected |= metricOptions[i];
            if (selected == 0)
            {
                message.Text = Localization.T("settings.selectMetric");
                return;
            }
            var value = new NotchPreferences((EdgeSide)edge.SelectedIndex, (NotchDisplayMode)mode.SelectedIndex)
            {
                RefreshIntervalMs = intervals[refresh.SelectedIndex],
                Sensitivity = (HoverSensitivity)sensitivity.SelectedIndex,
                Metrics = selected,
                SelectedDrive = (_drive.SelectedItem as DriveChoice)?.Name,
                StartAtLogin = autostart.IsChecked == true,
                Language = (language.SelectedItem as LanguageChoice)?.Value
            };
            var languageChanged = value.Language != current.Language;
            try
            {
                if (savePreferences is not null) savePreferences(value);
                else PreferenceStore.Save(storagePath ?? PreferenceStore.DefaultPath, value);
                apply(value);
                if (languageChanged)
                {
                    reopen?.Invoke();
                    Close();
                    return;
                }
                message.Text = value.Mode == NotchDisplayMode.Hidden
                    ? (hasTray ? Localization.T("settings.hiddenWithTray")
                        : Localization.T("settings.hiddenNoTray"))
                    : Localization.T("settings.saved");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                message.Text = Localization.T("settings.saveFailed");
            }
        };
        Content = new ScrollViewer { Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = Localization.T("settings.customize"), FontSize = 22 },
                new TextBlock { Text = Localization.T("settings.screenEdge") }, edge,
                new TextBlock { Text = Localization.T("settings.displayMode") }, mode,
                new TextBlock { Text = Localization.T("settings.visibleMetrics") }, metricPanel,
                new TextBlock { Text = Localization.T("settings.dataRefresh") }, refresh,
                new TextBlock { Text = Localization.T("settings.sensitivityLabel") }, sensitivity,
                new TextBlock
                {
                    Text = Localization.T("settings.sensitivityHint"),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new TextBlock { Text = Localization.T("settings.driveLabel") }, _drive,
                new TextBlock
                {
                    Text = Localization.T("settings.driveHint"),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new TextBlock { Text = Localization.T("settings.languageLabel") }, language,
                autostart,
                message, applyButton,
                new StackPanel { Children = { exitButton } }
            }
        } };
    }
    public void UpdateDrives(IReadOnlyList<DriveSnapshot> drives)
    {
        var selected = _drive.SelectedItem is DriveChoice current ? current.Name : _initialDrive;
        var choices = DriveSelection.Choices(drives, selected);
        if (_drive.ItemsSource is IReadOnlyList<DriveChoice> old && old.SequenceEqual(choices)) return;
        _drive.ItemsSource = choices;
        _drive.SelectedItem = choices.First(x => DriveSelection.PathComparer.Equals(x.Name, selected));
    }
}
