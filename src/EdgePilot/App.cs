using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using EdgePilot.UI;
using EdgePilot.Platform;
using Avalonia.Threading;

namespace EdgePilot;

public sealed class App : Application
{
    private TrayIcon? _tray;
    public override void Initialize()
    {
        var italian = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = italian;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = italian;
        System.Globalization.CultureInfo.CurrentCulture = italian;
        System.Globalization.CultureInfo.CurrentUICulture = italian;
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = OperatingSystem.IsLinux() ? ThemeVariant.Default : ThemeVariant.Dark;
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
                warning = "Impossibile leggere le impostazioni salvate. Sono attive quelle predefinite. Premi Applica per salvare nuovamente le preferenze.";
            }
            // Preserve the existing development override without writing it to disk.
            if (Environment.GetEnvironmentVariable("EDGEPILOT_EDGE") is { Length: > 0 })
                preferences = preferences with { Edge = EdgePlacement.FromEnvironment() };
            var registration = new AutostartRegistration();
            try { preferences = preferences with { StartAtLogin = registration.Read() is not null }; }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                warning = "Non è stato possibile verificare l’avvio automatico. Controlla le impostazioni.";
            }
            var window = new EdgeWindow();
            window.SavePreferences = value => DesktopPreferences.Save(PreferenceStore.DefaultPath,
                value, registration, LaunchCommand.Current(), OperatingSystem.IsWindows());
            window.ExitRequested += () => desktop.Shutdown();
            try
            {
                var menu = new NativeMenu();
                var settings = new NativeMenuItem { Header = "Impostazioni" };
                settings.Click += (_, _) => window.ShowSettings();
                var toggle = new NativeMenuItem { Header = "Mostra / Nascondi pannello" };
                toggle.Click += (_, _) => window.ToggleVisibility();
                var exit = new NativeMenuItem { Header = "Esci" };
                exit.Click += (_, _) => desktop.Shutdown();
                menu.Items.Add(settings);
                menu.Items.Add(toggle);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(exit);
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
                warning = "Icona nell’area di notifica non disponibile. Puoi riaprire le impostazioni avviando nuovamente EdgePilot.";
            }
            SingleInstance.Bind(() => Dispatcher.UIThread.Post(() => window.ShowSettings()));
            window.ApplyPreferences(preferences);
            desktop.MainWindow = window;
            window.Opened += (_, _) =>
            {
                if ((preferences.Mode == NotchDisplayMode.Hidden &&
                    (!window.HasTray || desktop.Args?.Contains("--autostart") != true)) || warning is not null ||
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
