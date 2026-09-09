using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using EdgePilot.Core;
using EdgePilot.UI;
using EdgePilot.UI.Signals;
using EdgePilot.Platform;
using Avalonia.Threading;

namespace EdgePilot;

public sealed class App : Application
{
    private TrayIcon? _tray;
    private NativeMenuItem? _settingsMenuItem;
    private NativeMenuItem? _toggleMenuItem;
    private NativeMenuItem? _exitMenuItem;
    private SignalCoordinator? _signals;

    public override void Initialize()
    {
        Localization.SetLanguage(Localization.DetectSystemLanguage());
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = ThemeVariant.Default;
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
                System.Diagnostics.Trace.WriteLine(ex);
                warning = Localization.T("warning.loadFailed");
            }
            Localization.SetLanguage(preferences.Language ?? Localization.DetectSystemLanguage());
            // Preserve the existing development override without writing it to disk.
            if (Environment.GetEnvironmentVariable("EDGEPILOT_EDGE") is { Length: > 0 })
                preferences = preferences with { Edge = EdgePlacement.FromEnvironment() };
            var registration = new AutostartRegistration();
            try { preferences = preferences with { StartAtLogin = registration.Read() is not null }; }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                warning = Localization.T("warning.autostartCheckFailed");
            }
            var window = new EdgeWindow();
            window.SavePreferences = value => DesktopPreferences.Save(PreferenceStore.DefaultPath,
                value, registration, LaunchCommand.Current(), OperatingSystem.IsWindows());
            window.ExitRequested += () => desktop.Shutdown();
            try
            {
                var menu = new NativeMenu();
                _settingsMenuItem = new NativeMenuItem { Header = Localization.T("tray.settings") };
                _settingsMenuItem.Click += (_, _) => window.ShowSettings();
                _toggleMenuItem = new NativeMenuItem { Header = Localization.T("tray.toggle") };
                _toggleMenuItem.Click += (_, _) => window.ToggleVisibility();
                _exitMenuItem = new NativeMenuItem { Header = Localization.T("tray.exit") };
                _exitMenuItem.Click += (_, _) => desktop.Shutdown();
                menu.Items.Add(_settingsMenuItem);
                menu.Items.Add(_toggleMenuItem);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(_exitMenuItem);
                _tray = new TrayIcon { Icon = AppIcon.Load(), ToolTipText = "EdgePilot", Menu = menu, IsVisible = true };
                _tray.Clicked += (_, _) => window.ShowSettings();
                TrayIcon.SetIcons(this, new TrayIcons { _tray });
                window.HasTray = _tray.NativeMenuExporter is not null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                _tray?.Dispose();
                _tray = null;
                window.HasTray = false;
                warning = Localization.T("warning.trayUnavailable");
            }
            window.PreferencesChanged += () =>
            {
                if (_settingsMenuItem is null) return;
                _settingsMenuItem.Header = Localization.T("tray.settings");
                _toggleMenuItem!.Header = Localization.T("tray.toggle");
                _exitMenuItem!.Header = Localization.T("tray.exit");
            };
            SingleInstance.Bind(() => Dispatcher.UIThread.Post(() => window.ShowSettings()));
            window.ApplyPreferences(preferences);

            _signals = new SignalCoordinator(window);
            _signals.Start();
            window.Closed += (_, _) =>
            {
                _signals?.Dispose();
                _signals = null;
            };

            desktop.MainWindow = window;
            window.Opened += (_, _) =>
            {
                var signalDemo = desktop.Args?.Contains("--signal-demo") == true ||
                    desktop.Args?.Contains("--signal-smoke-test") == true;
                if (signalDemo) _signals?.RunDemo();

                if (desktop.Args?.Contains("--signal-smoke-test") == true)
                {
                    DispatcherTimer.RunOnce(() =>
                    {
                        if (_signals is not null && _signals.HasSafePassiveInput)
                        {
                            Console.WriteLine("PASS Signal passive native input routing established.");
                            return;
                        }

                        Console.Error.WriteLine("EdgePilot Signal could not establish passive native input routing.");
                        Environment.Exit(3);
                    }, TimeSpan.FromSeconds(2));

                    // This is a disposable CI-only process. A transient secondary Avalonia window
                    // can keep the desktop lifetime alive after MainWindow is hidden, so end the
                    // smoke process explicitly after the full lost -> restored demo sequence.
                    DispatcherTimer.RunOnce(() => Environment.Exit(0), TimeSpan.FromSeconds(9));
                }

                if (desktop.Args?.Contains("--smoke-test") == true)
                {
                    if ((OperatingSystem.IsWindows() || OperatingSystem.IsLinux()) && !window.HasSafePlatformInput)
                    {
                        Console.Error.WriteLine("EdgePilot could not establish a safe native input region.");
                        Environment.ExitCode = 2;
                        // Shutting down synchronously from Opened can tear down Avalonia while its
                        // desktop lifetime is still entering StartCore. Defer by one dispatcher turn.
                        DispatcherTimer.RunOnce(() => desktop.Shutdown(), TimeSpan.FromMilliseconds(1));
                        return;
                    }
                    DispatcherTimer.RunOnce(() => desktop.Shutdown(), TimeSpan.FromSeconds(8));
                }

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
