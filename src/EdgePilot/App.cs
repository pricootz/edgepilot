using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using EdgePilot.UI;
using EdgePilot.Platform;
using EdgePilot.Localization;
using Avalonia.Threading;

namespace EdgePilot;

public sealed class App : Application
{
    private TrayIcon? _tray;
    public override void Initialize()
    {
        // The saved language is not readable yet, so start from the system one.
        // OnFrameworkInitializationCompleted applies the stored preference.
        Strings.Use(null);
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = OperatingSystem.IsLinux() ? ThemeVariant.Default : ThemeVariant.Dark;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            NotchPreferences preferences;
            var unreadable = false;
            try { preferences = PreferenceStore.Load(PreferenceStore.DefaultPath); }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException
                or System.Text.Json.JsonException or ArgumentException)
            {
                preferences = new();
                System.Diagnostics.Trace.WriteLine(ex);
                unreadable = true;
            }
            // The stored language governs every message from here on, so it is
            // applied before the first one is composed.
            Strings.Use(preferences.Language);
            string? warning = unreadable ? Strings.Get("app.warning.settings") : null;
            // Preserve the existing development override without writing it to disk.
            if (Environment.GetEnvironmentVariable("EDGEPILOT_EDGE") is { Length: > 0 })
                preferences = preferences with { Edge = EdgePlacement.FromEnvironment() };
            var registration = new AutostartRegistration();
            try { preferences = preferences with { StartAtLogin = registration.Read() is not null }; }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                warning = Strings.Get("app.warning.autostart");
            }
            var window = new EdgeWindow();
            window.SavePreferences = value => DesktopPreferences.Save(PreferenceStore.DefaultPath,
                value, registration, LaunchCommand.Current(), OperatingSystem.IsWindows());
            window.ExitRequested += () => desktop.Shutdown();
            try
            {
                var menu = new NativeMenu();
                var settings = new NativeMenuItem { Header = Strings.Get("tray.settings") };
                settings.Click += (_, _) => window.ShowSettings();
                var toggle = new NativeMenuItem { Header = Strings.Get("tray.toggle") };
                toggle.Click += (_, _) => window.ToggleVisibility();
                var exit = new NativeMenuItem { Header = Strings.Get("tray.exit") };
                exit.Click += (_, _) => desktop.Shutdown();
                menu.Items.Add(settings);
                menu.Items.Add(toggle);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(exit);
                _tray = new TrayIcon { Icon = AppIcon.Load(), ToolTipText = Strings.Get("tray.tooltip"), Menu = menu, IsVisible = true };
                _tray.Clicked += (_, _) => window.ShowSettings();
                // The menu is written once, so it is rewritten when the language moves.
                Strings.Changed += () =>
                {
                    settings.Header = Strings.Get("tray.settings");
                    toggle.Header = Strings.Get("tray.toggle");
                    exit.Header = Strings.Get("tray.exit");
                    if (_tray is not null) _tray.ToolTipText = Strings.Get("tray.tooltip");
                };
                TrayIcon.SetIcons(this, new TrayIcons { _tray });
                window.HasTray = _tray.NativeMenuExporter is not null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                _tray?.Dispose();
                _tray = null;
                window.HasTray = false;
                warning = Strings.Get("app.warning.tray");
            }
            SingleInstance.Bind(() => Dispatcher.UIThread.Post(() => window.ShowSettings()));
            window.ApplyPreferences(preferences);
            desktop.MainWindow = window;
            window.Opened += (_, _) =>
            {
                if (desktop.Args?.Contains("--smoke-test") == true)
                    DispatcherTimer.RunOnce(() => desktop.Shutdown(), TimeSpan.FromSeconds(8));
                if ((preferences.Mode == NotchDisplayMode.Hidden &&
                    (!window.HasTray || desktop.Args?.Contains("--autostart") != true)) || warning is not null ||
                    desktop.Args?.Contains("--settings") == true || desktop.Args?.Contains("--smoke-test") == true)
                {
                    window.ShowSettings(warning);
                    warning = null;
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
