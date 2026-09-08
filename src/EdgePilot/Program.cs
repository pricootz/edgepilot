using Avalonia;
using EdgePilot.Platform;
using EdgePilot.UI;
using EdgePilot.Localization;

namespace EdgePilot;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--version"))
        {
            Console.WriteLine("EdgePilot " + typeof(Program).Assembly.GetName().Version);
            return;
        }
        // Console messages are written before Avalonia exists, so the stored
        // language has to be applied here too.
        try { Strings.Use(PreferenceStore.Load(PreferenceStore.DefaultPath).Language); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or System.Text.Json.JsonException or ArgumentException or InvalidDataException)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            Strings.Use(null);
        }
        if (args.Contains("--install"))
        {
            try { Console.WriteLine(Strings.Get("cli.installed", DesktopInstaller.Install())); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
            {
                Console.Error.WriteLine(Strings.Get("cli.install.failed", ex.Message));
                Environment.ExitCode = 1;
            }
            return;
        }
        using var instance = new SingleInstance();
        if (!instance.IsPrimary)
        {
            if (!args.Contains("--autostart") && !instance.NotifyPrimary())
            {
                Console.Error.WriteLine(Strings.Get("cli.already.running"));
                Environment.ExitCode = 1;
            }
            return;
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}
