using Avalonia;
using EdgePilot.Core;
using EdgePilot.Platform;

namespace EdgePilot;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Localization.SetLanguage(Localization.DetectSystemLanguage());
        if (args.Contains("--version"))
        {
            Console.WriteLine("EdgePilot " + typeof(Program).Assembly.GetName().Version);
            return;
        }
        if (args.Contains("--install"))
        {
            try { Console.WriteLine(Localization.T("cli.installedTo", DesktopInstaller.Install())); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
            {
                Console.Error.WriteLine(Localization.T("cli.installFailed", ex.Message));
                Environment.ExitCode = 1;
            }
            return;
        }
        using var instance = new SingleInstance();
        if (!instance.IsPrimary)
        {
            if (!args.Contains("--autostart") && !instance.NotifyPrimary())
            {
                Console.Error.WriteLine(Localization.T("cli.alreadyRunning"));
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
