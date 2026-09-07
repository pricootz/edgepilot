using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using EdgePilot.UI;

namespace EdgePilot;

public sealed class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Dark;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            NotchPreferences preferences;
            string? warning = null;
            try { preferences = PreferenceStore.Load(PreferenceStore.DefaultPath); }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or ArgumentException)
            {
                preferences = new();
                warning = "Could not read saved settings. Defaults are active; Apply replaces the file. " + ex.Message;
            }
            // Preserve the existing development override without writing it to disk.
            if (Environment.GetEnvironmentVariable("EDGEPILOT_EDGE") is { Length: > 0 })
                preferences = preferences with { Edge = EdgePlacement.FromEnvironment() };
            var window = new EdgeWindow();
            window.ApplyPreferences(preferences);
            desktop.MainWindow = window;
            window.Opened += (_, _) =>
            {
                if (preferences.Mode == NotchDisplayMode.Hidden || warning is not null ||
                    desktop.Args?.Contains("--settings") == true)
                {
                    window.ShowSettings(warning);
                    warning = null;
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
